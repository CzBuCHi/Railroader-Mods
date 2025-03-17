using System;
using CarInspectorTweaks.UI;
using CzBuCHi.Shared.Harmony;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using UI.Builder;

namespace CarInspectorTweaks;

[UsedImplicitly]
public sealed class CarInspectorTweaksPlugin : SingletonPluginBase<CarInspectorTweaksPlugin>, IModTabHandler
{
    private const string ModIdentifier = "CzBuCHi.CarInspectorTweaks";

    public static IModdingContext Context  { get; private set; } = null!;
    public static IUIHelper       UiHelper { get; private set; } = null!;
    public static Settings        Settings { get; private set; } = null!;

    private readonly ILogger   _Logger    = Log.ForContext<CarInspectorTweaksPlugin>()!;
    private          Messenger _Messenger = null!;

    public CarInspectorTweaksPlugin(IModdingContext context, IUIHelper uiHelper) {
        Context = context;
        UiHelper = uiHelper;
        Settings = Context.LoadSettingsData<Settings>(ModIdentifier) ?? new Settings();

        ProgrammaticWindowCreatorPatches.RegisterWindow<ConsistWindow>();
    }

    public override void OnEnable() {
        _Logger.Information("OnEnable");
        ApplyPatches();

        _Messenger = Messenger.Default!;
        _Messenger.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
    }

    private void OnMapDidLoad(MapDidLoadEvent obj) {
        CzBuCHi.Shared.UI.TopRightArea.AddButton("CarInspectorTweaks.icon.png", "Consist Window", 9, ConsistWindow.Shared.ShowWindow);
        if (Settings.AutoOpenConsistWindow) {
            ConsistWindow.Shared.ShowWindow();
        }
    }

    public override void OnDisable() {
        _Logger.Information("OnDisable");
        UnpatchAll();

        _Messenger.Unregister(this);
    }

    public static void SaveSettings() {
        Context.SaveSettingsData(ModIdentifier, Settings);
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        builder.AddField("Whistle Buttons", builder.AddToggle(() => Settings.WhistleButtons, o => Settings.WhistleButtons = o)!)!
               .Tooltip("Whistle Buttons", "Adds 'Prev' and 'Next' buttons under whistle drop down to simplify whistle selection.");

        builder.AddField("Faster strong man", builder.AddToggle(() => Settings.FastStrongMan, o => Settings.FastStrongMan = o)!)!
               .Tooltip("Faster strong man", "Engineer is able to push car up to 6mph.");

        builder.AddField("Copy repair destination", builder.AddToggle(() => Settings.CopyRepairDestination, o => Settings.CopyRepairDestination = o)!)!
               .Tooltip("Copy repair destination", "Adds 'Copy repair destination' button to equipment panel when car has repair destination selected.");

        builder.AddField("Show car speed", builder.AddToggle(() => Settings.ShowCarSpeed, o => Settings.ShowCarSpeed = o)!)!
               .Tooltip("Show car speed", "Shows car speed on car tab.");

        builder.AddField("Show car oil", builder.AddToggle(() => Settings.ShowCarOil, o => Settings.ShowCarOil = o)!)!
               .Tooltip("Show car oil", "Shows car oil state on car tab.");

        builder.AddField("Bleed all", builder.AddToggle(() => Settings.BleedAll, o => Settings.BleedAll = o)!)!
               .Tooltip("Bleed all", "Adds 'Bleed all' button to car panel.");

        builder.AddField("Update customize window", builder.AddToggle(() => Settings.UpdateCarCustomizeWindow, o => Settings.UpdateCarCustomizeWindow = o)!)!
               .Tooltip("Update customize window", "Updates car customize window when different car is selected.");

        builder.AddField("Manage Consist", builder.AddToggle(() => Settings.ConsistManage, o => {
            Settings.ConsistManage = o;
            builder.Rebuild();
        })!)!
               .Tooltip("Manage Consist", "Adds 'connect air', 'release handbrakes' and 'oil all cars' buttons to manual orders and yard tab.");

        if (Settings.ConsistManage) {
            builder.AddField("Oil Threshold", builder.AddSliderQuantized(
                       () => Settings.OilThreshold,
                       () => (Settings.OilThreshold * 100).ToString("0") + "%",
                       o => Settings.OilThreshold = o,
                       0.01f, 0, 1,
                       o => Settings.OilThreshold = o)!)!
                   .Tooltip("Oil Threshold", "Show 'oil all cars' button if any car has less oil than specified.");
        }

        builder.AddField("Yard max Speed", builder.AddSliderQuantized(
                   () => Settings.YardMaxSpeed,
                   () => Settings.YardMaxSpeed.ToString(),
                   o => Settings.YardMaxSpeed = (int)o,
                   5f, 0f, 45f,
                   o => Settings.YardMaxSpeed = (int)o)!)!
               .Tooltip("Yard max Speed", "Maximum allowed speed in yard mode");

        builder.AddField("Set car Inspector Height", builder.AddToggle(() => Settings.SetCarInspectorHeight, o => Settings.SetCarInspectorHeight = o)!)!
               .Tooltip("Set car Inspector Height", "Set car inspector height");

        builder.AddField("Car Inspector Height", builder.AddSliderQuantized(
                   () => Settings.CarInspectorHeight,
                   () => Settings.CarInspectorHeight.ToString(),
                   o => Settings.CarInspectorHeight = (int)o,
                   5f, 0f, 1000f,
                   o => Settings.CarInspectorHeight = (int)o)!);

        builder.AddField("Copy crew", builder.AddToggle(() => Settings.CopyCrew, o => Settings.CopyCrew = o)!)!
               .Tooltip("Copy crew", "Copy car's crew to the other cars in consist.");

        builder.AddField("Auto Open Consist Window", builder.AddToggle(() => Settings.AutoOpenConsistWindow, o => Settings.AutoOpenConsistWindow = o)!)!
               .Tooltip("Auto Open Consist Window", "Automatically open consist window on map load.");
        
        builder.AddButton("Open Consist Window", ConsistWindow.Shared.ShowWindow);
        builder.AddButton("Save", ModTabDidClose);
    }

    public void ModTabDidClose() {
        SaveSettings();
        UnpatchAll();
        ApplyPatches();
    }

    private void UnpatchAll() {
        var harmony = new HarmonyLib.Harmony(ModIdentifier);
        harmony.UnpatchAll(ModIdentifier);
    }

    private void ApplyPatches() {
        var harmony = new HarmonyLib.Harmony(ModIdentifier);

        if (Settings.WhistleButtons) {
            harmony.PatchCategory("WhistleButtons");
        }

        if (Settings.FastStrongMan) {
            harmony.PatchCategory("FastStrongMan");
        }

        if (Settings.CopyRepairDestination) {
            harmony.PatchCategory("CopyRepairDestination");
        }

        if (Settings.ShowCarSpeed) {
            harmony.PatchCategory("ShowCarSpeed");
        }

        if (Settings.ShowCarOil) {
            harmony.PatchCategory("ShowCarOil");
        }

        if (Settings.BleedAll) {
            harmony.PatchCategory("BleedAll");
        }

        if (Settings.UpdateCarCustomizeWindow) {
            harmony.PatchCategory("UpdateCarCustomizeWindow");
        }

        if (Settings.ConsistManage) {
            harmony.PatchCategory("ConsistManage");
        }

        if (Settings.SetCarInspectorHeight) {
            harmony.PatchCategory("CarInspectorHeight");
        }

        if (Settings.CopyCrew) {
            harmony.PatchCategory("CopyCrew");
        }

        harmony.PatchCategory("ConsistWindow");
    }
}
