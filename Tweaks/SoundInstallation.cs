using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game;
using Game.State;
using HarmonyLib;
using KeyValue.Runtime;
using Model;
using Model.Ops;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;

namespace Tweaks;

internal static class SoundInstallation
{
    internal const string Key = "czbuchi.tweaks.soundInstallation";
    internal const string Tag = "Sound installation";
    internal static bool Pending(Car car) => car != null && !car.KeyValueObject[Key].IsNull;
    internal static bool VisiblePending(Car car) => Pending(car) && !car.KeyValueObject[Key]["prank"].BoolValue;
    private static void PrepareManual(Car car)
    {
        if (Pending(car) && car.KeyValueObject[Key]["prank"].BoolValue)
            Finish(car, car.KeyValueObject[Key], false);
        // A deliberate choice wins over prankster for this repair-track visit.
        car.KeyValueObject[RepairSounds.LastTrackKey] = RepairSounds.TrackFor(car) ?? string.Empty;
    }
    internal static string Status(Car car)
    {
        var data = car.KeyValueObject[Key];
        if (data.IsNull) return "Complete";
        var seconds = Math.Max(0, (int)Math.Ceiling(data["remaining"].FloatValue));
        var state = car.VelocityMphAbs < 0.1f ? "remaining" : "paused: stop the locomotive";
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") + " " + state;
    }
    internal static void QueueWhistle(Car car, string id)
        {
        if (!RepairSounds.CanChange(car) || VisiblePending(car)) return;
        PrepareManual(car);
        if (id == RepairSounds.CurrentWhistle(car)) return;
        if (TweaksPlugin.Settings.ManualSoundInstallationDelay) Queue(car, id, string.Empty, false);
        else RepairSounds.SetWhistle(car, id);
    }
    internal static void QueueBell(Car car, string id)
        {
        if (!RepairSounds.CanChange(car) || VisiblePending(car)) return;
        PrepareManual(car);
        if (id == RepairSounds.CurrentBell(car)) return;
        if (TweaksPlugin.Settings.ManualSoundInstallationDelay) Queue(car, string.Empty, id, false);
        else RepairSounds.SetBell(car, id);
    }
    internal static void QueuePrank(Car car) => Queue(car, string.Empty, string.Empty, true);
    private static void Queue(Car car, string whistle, string bell, bool prank)
    {
        if (Pending(car) || !RepairSounds.CanChange(car)) return;
        var track = RepairSounds.TrackFor(car);
        if (track == null || !OpsController.Shared.TryGetIndustryComponent<RepairTrack>(track, out var repair)) return;
        var data = new Dictionary<string, Value> {
            { "track", track }, { "whistle", whistle }, { "bell", bell }, { "prank", prank },
            { "remaining", ShopWorkTiming.Duration },
            { "last", TimeWeather.Now.TotalSeconds.ToString("R", CultureInfo.InvariantCulture) },
            { "previousWaybill", car.KeyValueObject[Car.KeyOpsWaybill] }
        };
        car.KeyValueObject[Key] = Value.Dictionary(data);
        if (!prank) car.SetWaybill(new Waybill(TimeWeather.Now, null, repair, 0, false, Tag, 0));
    }
    internal static void Tick(Car car)
    {
        var value = car.KeyValueObject[Key]; if (value.IsNull) return;
        // Hide an older saved prank work order as well.
        if (value["prank"].BoolValue && car.Waybill.HasValue && car.Waybill.Value.Tag == Tag)
            car.KeyValueObject[Car.KeyOpsWaybill] = value["previousWaybill"];
        var track = value["track"].StringValue;
        if (RepairSounds.TrackFor(car) != track) { Finish(car, value, false); return; }
        if (!value["prank"].BoolValue && !TweaksPlugin.Settings.ManualSoundInstallationDelay)
        { Finish(car, value, true); return; }
        var now = TimeWeather.Now.TotalSeconds;
        if (!double.TryParse(value["last"].StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var last)) last = now;
        var remaining = ShopWorkTiming.Remaining(value["remaining"].FloatValue, last, now, car.VelocityMphAbs < 0.1f);
        if (remaining <= 0f) { Finish(car, value, true); return; }
        var data = value.DictionaryValue.ToDictionary(pair => pair.Key, pair => pair.Value);
        data["remaining"] = remaining; data["last"] = now.ToString("R", CultureInfo.InvariantCulture);
        car.KeyValueObject[Key] = Value.Dictionary(data);
    }
    private static void Finish(Car car, Value value, bool complete)
    {
        if (complete)
        {
            if (value["prank"].BoolValue) RepairSounds.Randomize(car);
            else
            {
                var whistle = value["whistle"].StringValue; var bell = value["bell"].StringValue;
                if (!string.IsNullOrEmpty(whistle)) RepairSounds.SetWhistle(car, whistle);
                if (!string.IsNullOrEmpty(bell)) RepairSounds.SetBell(car, bell);
            }
            // A manual installation also prevents an immediate prank on the same visit.
            car.KeyValueObject[RepairSounds.LastTrackKey] = value["track"];
            Debug.Log("[Tweaks] Completed sound installation on " + car.DisplayName);
        }
        if (car.Waybill.HasValue && car.Waybill.Value.Tag == Tag)
            car.KeyValueObject[Car.KeyOpsWaybill] = value["previousWaybill"];
        car.KeyValueObject[Key] = Value.Null();
    }
    internal static void CancelAll()
    {
        if (!StateManager.IsHost || TrainController.Shared?.Cars == null) return;
        foreach (var car in TrainController.Shared.Cars.Where(Pending).ToArray()) Finish(car, car.KeyValueObject[Key], false);
    }
}

// Arrival must not complete the installation waybill before the shop timer.
[HarmonyPatch(typeof(IndustryComponent), "OnCompleteWaybill")]
internal static class InstallationWaybillPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Waybill waybill) => waybill.Tag != SoundInstallation.Tag;
}

[HarmonyPatch(typeof(CarInspector), "PopulateEquipmentPanel")]
internal static class InstallationInspectorPatch
{
    [HarmonyPostfix]
    private static void Postfix(UIPanelBuilder builder, Car ____car)
    {
        var car = ____car; if (!(car is BaseLocomotive)) return;
        var pending = SoundInstallation.VisiblePending(car);
        builder.AddObserver(car.KeyValueObject.Observe(SoundInstallation.Key, value => {
            var visible = !value.IsNull && !value["prank"].BoolValue; if (visible != pending) { pending = visible; builder.Rebuild(); }
        }, false));
        if (!pending) return;
        builder.AddField("Shop work", () => SoundInstallation.Status(car), UIPanelBuilder.Frequency.Periodic);
        builder.AddField("Work order", "Sound installation");
    }
}
