using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
using Model.Definition.Components;
using Model.Definition.Data;
using Model.Ops;
using RollingStock.Steam;
using UI.Builder;
using UI.CarCustomizeWindow;
using UnityEngine;

namespace Tweaks;

internal static class RepairSounds
{
    internal const string BellKey = "rf.bell.custom";
    internal const string LastTrackKey = "czbuchi.tweaks.soundPrank.lastRepairTrack";
    internal static readonly Type BellRuntime = typeof(RailForge.PluginBase).Assembly.GetType("RailForge.RailForgeBellRuntime", true)!;
    private static readonly MethodInfo Profiles = AccessTools.Method(BellRuntime, "ProfileNames");
    private static readonly MethodInfo Compatible = AccessTools.Method(BellRuntime, "CompatibilityEnabled");
    private static readonly MethodInfo ForeignOwner = AccessTools.Method(BellRuntime, "HasForeignPlaybackOwner");
    private static OpsController? cachedOps;
    private static float nextScan;
    private static readonly Dictionary<Car, string> tracks = new Dictionary<Car, string>();
    internal static bool Ready => !StateManager.IsUnloading && OpsController.Shared != null && OpsController.Shared.AllIndustries != null;
    internal static string? TrackFor(Car car)
    {
        if (!Ready || car == null || car.set == null) return null;
        var ops = OpsController.Shared;
        if (cachedOps != ops || Time.unscaledTime >= nextScan)
        {
            cachedOps = ops; nextScan = Time.unscaledTime + 0.5f; tracks.Clear();
            foreach (var repair in ops.AllIndustries.Where(industry => industry != null)
                .SelectMany(industry => industry.Components).OfType<RepairTrack>().OrderBy(repair => repair.Identifier))
                foreach (var item in ops.CarsAtPosition(repair))
                    if (item != null && !tracks.ContainsKey(item)) tracks.Add(item, repair.Identifier);
        }
        return tracks.TryGetValue(car, out var track) ? track : null;
    }
    internal static bool CanChange(Car car) => car != null && CarCustomizeWindow.CanCustomize(car, out _) && TrackFor(car) != null;
    internal static List<(string Id, string Name)> Whistles(Car car)
    {
        if (!car.Definition.Components.OfType<WhistleComponent>().Any()) return new List<(string, string)>();
        return TrainController.Shared.PrefabStore.AllDefinitionInfosOfType<WhistleDefinition>()
            .Select(item => (item.Identifier, item.Metadata.Name)).ToList();
    }
    internal static string CurrentWhistle(Car car)
    {
        var component = car.Definition.Components.OfType<WhistleComponent>().FirstOrDefault();
        return component == null ? string.Empty : (WhistleCustomizationSettings.FromPropertyValue(car.KeyValueObject["whistle.custom"])
            ?? new WhistleCustomizationSettings(component.DefaultWhistleIdentifier)).WhistleIdentifier;
    }
    internal static List<string> Bells(Car car)
    {
        if (!car.Definition.Components.OfType<BellComponent>().Any() || !(bool)Compatible.Invoke(null, null) || (bool)ForeignOwner.Invoke(null, null))
            return new List<string>();
        return ((IList<string>)Profiles.Invoke(null, null)).ToList();
    }
    internal static string CurrentBell(Car car)
    {
        var value = car.KeyValueObject[BellKey].StringValue;
        return string.IsNullOrEmpty(value) ? "Default" : value;
    }
    internal static void SetWhistle(Car car, string id) => car.KeyValueObject["whistle.custom"] = new WhistleCustomizationSettings(id).PropertyValue;
    internal static void SetBell(Car car, string name) => StateManager.ApplyLocal(new PropertyChange(car.id, BellKey, new StringPropertyValue(name)));
    internal static int RandomOther(int count, int current)
    {
        if (count <= 1) return 0;
        if (current < 0 || current >= count) return UnityEngine.Random.Range(0, count);
        var index = UnityEngine.Random.Range(0, count - 1); return index >= current ? index + 1 : index;
    }
    internal static bool Randomize(Car car)
    {
        var changed = false;
        var whistles = Whistles(car); var currentWhistle = whistles.FindIndex(item => item.Id == CurrentWhistle(car));
        if (whistles.Count > 1)
        { SetWhistle(car, whistles[RandomOther(whistles.Count, currentWhistle)].Id); changed = true; }
        var bells = Bells(car); var currentBell = bells.IndexOf(CurrentBell(car));
        if (bells.Count > 1)
        { SetBell(car, bells[RandomOther(bells.Count, currentBell)]); changed = true; }
        if (PrankLettering.Randomize(car)) changed = true;
        return changed;
    }
    internal static void Reset() { cachedOps = null; nextScan = 0f; tracks.Clear(); }

    internal static void Build(UIPanelBuilder builder, Car car, string title, List<string> names, int current, Action<int> change)
    {
        if (names.Count == 0) return;
        var canCustomize = CarCustomizeWindow.CanCustomize(car, out _);
        if (!canCustomize)
        {
            builder.AddField(title, current >= 0 && current < names.Count ? names[current] : "Default");
            return;
        }
        var onRepairTrack = TrackFor(car) != null;
        var dropdown = builder.AddDropdown(names, current, Select);
        dropdown.GetComponent<TMPro.TMP_Dropdown>().interactable = onRepairTrack && !SoundInstallation.VisiblePending(car);
        builder.AddField(title, dropdown);
        if (!onRepairTrack) builder.AddLabel("Sound changes require a repair track.");
        if (SoundInstallation.VisiblePending(car)) builder.AddField("Installation", () => SoundInstallation.Status(car), UIPanelBuilder.Frequency.Periodic);
        void Select(int index)
        {
            // Position is checked again when clicked; the locomotive may have left.
            if (index < 0 || index >= names.Count || !CanChange(car) || SoundInstallation.VisiblePending(car)) return;
            change(index); builder.Rebuild();
        }
    }
}

[HarmonyPatch(typeof(CarCustomizeWindow), "BuildSoundTabWhistle")]
internal static class WhistleSelectionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(UIPanelBuilder builder, Car ____car)
    {
        if (!TweaksPlugin.Settings.RepairSoundButtons) return true;
        var choices = RepairSounds.Whistles(____car);
        var car = ____car;
        if (choices.Count == 0) return false;
        var current = choices.FirstOrDefault(item => item.Id == RepairSounds.CurrentWhistle(car)).Name ?? "Default";
        builder.AddField("Whistle", current);
        if (CarCustomizeWindow.CanCustomize(car, out _))
        {
            builder.AddButton("Choose / test whistles", () => WhistlePicker.Open(car));
            if (RepairSounds.TrackFor(car) == null) builder.AddLabel("Sound changes require a repair track.");
            if (SoundInstallation.VisiblePending(car))
                builder.AddField("Installation", () => SoundInstallation.Status(car), UIPanelBuilder.Frequency.Periodic);
        }
        return false;
    }
}

// Replace only RailForge's bell selector, preserving its playback and catalog.
[HarmonyPatch]
internal static class BellSelectionPatch
{
    private static MethodBase TargetMethod() => AccessTools.Method(RepairSounds.BellRuntime, "CarCustomizeWindowBuildSoundTabPostfix");
    [HarmonyPrefix]
    private static bool Prefix(UIPanelBuilder __0, Car __1)
    {
        if (!TweaksPlugin.Settings.RepairSoundButtons) return true;
        if (__1 == null) return false;
        var car = __1; var choices = RepairSounds.Bells(car);
        if (choices.Count > 1)
            __0.AddSection("Bell", section => RepairSounds.Build(section, car, "Bell", choices,
                choices.IndexOf(RepairSounds.CurrentBell(car)), index => SoundInstallation.QueueBell(car, choices[index])));
        return false;
    }
}

[HarmonyPatch(typeof(CarCustomizeWindow), "OnEnable")]
internal static class RepairSoundsWindowPatch
{
    [HarmonyPostfix]
    private static void Postfix(CarCustomizeWindow __instance)
    {
        var refresh = __instance.GetComponent<RepairSoundsWindowRefresh>() ?? __instance.gameObject.AddComponent<RepairSoundsWindowRefresh>();
        refresh.Initialize(__instance);
    }
}

internal sealed class RepairSoundsWindowRefresh : MonoBehaviour
{
    private static readonly FieldInfo CarField = AccessTools.Field(typeof(CarCustomizeWindow), "_car");
    private static readonly MethodInfo Populate = AccessTools.Method(typeof(CarCustomizeWindow), "Populate");
    private CarCustomizeWindow? owner;
    private string? signature;
    private float nextScan;
    internal void Initialize(CarCustomizeWindow window) { owner = window; signature = null; nextScan = 0f; }
    private void Update()
    {
        if (owner == null || !owner.isActiveAndEnabled || Time.unscaledTime < nextScan || !RepairSounds.Ready) return;
        nextScan = Time.unscaledTime + 0.5f;
        var car = (Car)CarField.GetValue(owner); if (car == null || car.set == null) return;
        var current = car.id + "|" + TweaksPlugin.Settings.RepairSoundButtons + "|" + RepairSounds.CanChange(car)
            + "|" + SoundInstallation.VisiblePending(car) + "|" + RepairSounds.CurrentWhistle(car) + "|" + RepairSounds.CurrentBell(car);
        var changed = signature != null && signature != current; signature = current;
        if (changed) Populate.Invoke(owner, null);
    }
}

internal sealed class RepairSoundPrank : MonoBehaviour
{
    private float nextScan;
    private bool reportedFailure;
    private void Update()
    {
        if (!StateManager.IsHost || !RepairSounds.Ready || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.5f;
        var controller = TrainController.Shared;
        if (controller == null || controller.Cars == null) return;
        try
        {
            foreach (var locomotive in controller.Cars.OfType<BaseLocomotive>())
            {
                if (locomotive == null || locomotive.set == null) continue;
                if (SoundInstallation.Pending(locomotive)) { SoundInstallation.Tick(locomotive); continue; }
                if (!TweaksPlugin.Settings.RepairSoundPrank || !locomotive.IsOwnedByPlayer || locomotive.VelocityMphAbs >= 0.1f) continue;
                var previous = locomotive.KeyValueObject[RepairSounds.LastTrackKey].StringValue;
                var state = new RepairStopState(previous);
                var track = RepairSounds.TrackFor(locomotive);
                var trigger = state.Observe(true, track);
                if (trigger)
                {
                    if (!RepairSounds.CanChange(locomotive)) continue;
                    SoundInstallation.QueuePrank(locomotive);
                    continue;
                }
                if (state.LastTrack != (string.IsNullOrEmpty(previous) ? null : previous))
                    StateManager.ApplyLocal(new PropertyChange(locomotive.id, RepairSounds.LastTrackKey,
                        new StringPropertyValue(state.LastTrack ?? string.Empty)));
            }
        }
        catch (Exception error)
        {
            if (!reportedFailure) { Debug.LogError("[Tweaks] Repair shop prank failed: " + error); reportedFailure = true; }
            nextScan = Time.unscaledTime + 5f;
        }
    }
}
