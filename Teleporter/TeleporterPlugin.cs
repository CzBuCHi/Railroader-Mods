using System;
using CzBuCHi.Shared.Harmony;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using Teleporter.Events;
using Teleporter.TopRightArea;
using Teleporter.UI;
using UI.Builder;

namespace Teleporter;

[PublicAPI]
public sealed class TeleporterPlugin : SingletonPluginBase<TeleporterPlugin>, IModTabHandler
{
    private const string PluginIdentifier = "CzBuCHi.Teleporter";

    private readonly IModdingContext _Context;
    private readonly Settings       _Settings;

    public TeleporterPlugin(IModdingContext context, IUIHelper uiHelper) {
        _Context = context;
        _Settings = _Context.LoadSettingsData<Settings>(PluginIdentifier) ?? new Settings();
    }
    
    private Messenger _Messenger = null!;

    public static Settings Settings => Shared!._Settings;

    public static void SaveSettings() {
        Shared!._Context.SaveSettingsData(PluginIdentifier, Shared._Settings);
        SendSettingsChangedEvent();
    }

    public static void SendSettingsChangedEvent() {
        Shared!._Messenger.Send(new SettingsChangedEvent());
    }

    public override void OnEnable() {
        _Messenger = Messenger.Default!;
        _Messenger.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
        _Messenger.Register(this, new Action<MapDidUnloadEvent>(OnMapDidUnload));

        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.PatchAll();

        ProgrammaticWindowCreatorPatches.RegisterWindow<TeleporterWindow>();
    }

    public override void OnDisable() {
        _Messenger.Unregister(this);

        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.UnpatchAll();
    }

    private void OnMapDidLoad(MapDidLoadEvent @event) {
        TopRightAreaExtension.AddButton("icon.png", "Teleporter", 9, TeleporterWindow.Toggle);

        if (Settings.AutoOpenTeleporterWindow) {
            TeleporterWindow.Toggle();
        }
    }

    private void OnMapDidUnload(MapDidUnloadEvent @event) {
        SaveSettings();
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        builder.AddField("Auto Open Teleporter Window", builder.AddToggle(() => Settings.AutoOpenTeleporterWindow, o => Settings.AutoOpenTeleporterWindow = o)!)!
               .Tooltip("Auto Open Teleporter Window", "Automatically open teleporter window on map load.");
        
        builder.AddButton("Save", ModTabDidClose);
    }

    public void ModTabDidClose() {
        SaveSettings();
    }
}
