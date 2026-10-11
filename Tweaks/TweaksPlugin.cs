using System;
using HarmonyLib;
using RailForge;
using UI.Builder;
using UnityEngine;

namespace Tweaks;

public sealed class TweaksPlugin : PluginBase, IModTabHandler
{
    internal const string Identifier = "CzBuCHi.Tweaks";
    private readonly IModdingContext context;
    private bool enabled;
    private GameObject? soundPrank;
    internal static Settings Settings { get; private set; } = new Settings();
    public TweaksPlugin(IModdingContext context) { this.context = context; }

    public override void OnEnable()
    {
        if (enabled) return;
        Settings = context.LoadSettingsData<Settings>(Identifier) ?? new Settings();
        var harmony = new Harmony(Identifier);
        try
        {
            harmony.PatchAll(typeof(TweaksPlugin).Assembly);
            soundPrank = new GameObject(Identifier + ".RepairSoundPrank");
            UnityEngine.Object.DontDestroyOnLoad(soundPrank);
            soundPrank.AddComponent<RepairSoundPrank>();
            enabled = true;
            Debug.Log("[Tweaks] Enabled selected car and time tweaks.");
        }
        catch
        {
            harmony.UnpatchAll(Identifier);
            throw;
        }
    }
    public override void OnDisable()
    {
        SoundInstallation.CancelAll();
        new Harmony(Identifier).UnpatchAll(Identifier);
        foreach (var buttons in Resources.FindObjectsOfTypeAll<ControlModeButtons>())
        {
            buttons.enabled = false;
            UnityEngine.Object.Destroy(buttons);
        }
        foreach (var shortcut in Resources.FindObjectsOfTypeAll<WaypointShortcut>())
        {
            shortcut.enabled = false;
            UnityEngine.Object.Destroy(shortcut);
        }
        foreach (var actions in Resources.FindObjectsOfTypeAll<ConsistActions>())
        {
            actions.enabled = false;
            UnityEngine.Object.Destroy(actions);
        }
        if (soundPrank != null) { soundPrank.SetActive(false); UnityEngine.Object.Destroy(soundPrank); soundPrank = null; }
        foreach (var refresh in Resources.FindObjectsOfTypeAll<RepairSoundsWindowRefresh>())
        { refresh.enabled = false; UnityEngine.Object.Destroy(refresh); }
        WhistlePicker.DestroyWindow();
        PrankTextEditor.DestroyWindow();
        RepairSounds.Reset();
        enabled = false;
    }
    private void Save() => context.SaveSettingsData(Identifier, Settings);
    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        builder.AddFieldToggle("Sleep until next interchange", () => Settings.SleepUntilNextInterchange, value => { Settings.SleepUntilNextInterchange = value; Save(); });
        builder.AddFieldToggle("Show car speed", () => Settings.ShowCarSpeed, value => { Settings.ShowCarSpeed = value; Save(); });
        builder.AddFieldToggle("Show train length in meters", () => Settings.ShowTrainLengthInMeters, value => { Settings.ShowTrainLengthInMeters = value; Save(); });
        builder.AddFieldToggle("Show car oil", () => Settings.ShowCarOil, value => { Settings.ShowCarOil = value; Save(); });
        builder.AddFieldToggle("Follow and select (Shift + eye)", () => Settings.FollowAndSelect, value => { Settings.FollowAndSelect = value; Save(); });
        builder.AddFieldToggle("Fast strong man (6 mph)", () => Settings.FastStrongMan, value => { Settings.FastStrongMan = value; Save(); });
        builder.AddFieldToggle("Copy crew", () => Settings.CopyCrew, value => { Settings.CopyCrew = value; Save(); });
        builder.AddFieldToggle("Copy repair destination", () => Settings.CopyRepairDestination, value => { Settings.CopyRepairDestination = value; Save(); });
        builder.AddFieldToggle("Shift+G waypoint in every mode", () => Settings.WaypointShortcutAllModes, value => { Settings.WaypointShortcutAllModes = value; Save(); });
        builder.AddFieldToggle("Consist action buttons", () => Settings.ConsistActionButtons, value => { Settings.ConsistActionButtons = value; Save(); });
        builder.AddField("Oil warning threshold", builder.AddSliderQuantized(
            () => Settings.OilWarningThreshold, () => Mathf.RoundToInt(Settings.OilWarningThreshold * 100f) + "%",
            value => { Settings.OilWarningThreshold = value; Save(); }, 0.05f, 0f, 1f));
        builder.AddFieldToggle("Control mode icon buttons", () => Settings.ControlModeButtons, value => { Settings.ControlModeButtons = value; Save(); });
        builder.AddFieldToggle("Whistle and bell installation (repair track only)", () => Settings.RepairSoundButtons, value => { Settings.RepairSoundButtons = value; Save(); });
        builder.AddFieldToggle("Manual sound installation takes 5 game minutes", () => Settings.ManualSoundInstallationDelay, value => { Settings.ManualSoundInstallationDelay = value; Save(); });
        builder.AddFieldToggle("Repair shop prankster", () => Settings.RepairSoundPrank, value => { Settings.RepairSoundPrank = value; Save(); });
        builder.AddFieldToggle("Prankster changes lettering", () => Settings.PrankLettering, value => { Settings.PrankLettering = value; Save(); builder.Rebuild(); });
        if (Settings.PrankLettering) builder.AddButton("Edit prank texts", PrankTextEditor.Open);
        builder.AddFieldToggle("Bleed all", () => Settings.BleedAll, value => { Settings.BleedAll = value; Save(); });
    }
    public void ModTabDidClose() => Save();
}
