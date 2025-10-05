using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CarInspectorTweaks.Data;
using Game;
using Game.Notices;
using Game.State;
using HarmonyLib;
using JetBrains.Annotations;
using Model.Ops;
using Serilog;
using Serilog.Core;
using UI;
using UI.Builder;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace CarInspectorTweaks.Features;

public static class TimeNotificationsManager
{
    private static readonly ILogger _Logger = Log.ForContext(typeof(TimeNotificationsManager))!;

    private const float Tolerance = 1 / 3600.0f;

    private static int _Top;
    private static int _Day;

    private static TimeNotification[] Notifications => CarInspectorTweaksPlugin.Settings.TimeNotifications;

    private static bool _Initialized;

    private static void Initialize() {
        if (!_Initialized) {
            Reset();
            _Initialized = true;
        }
    }

    private static void Reset() {
        _Day = TimeWeather.Now.Day;
        _Top = 0;
    }

    public static void CheckTime() => CheckTime(TimeWeather.Now);

    public static void CheckTime(GameDateTime time) {
        _Logger.Information("CheckTime: " + time.TimeString());
        Initialize();
        if (_Day != time.Day) {
            _Day = time.Day;
            _Top = 0;
        }

        var hours = time.Hours;
        while (_Top < Notifications.Length && Mathf.Abs(Notifications[_Top].Hours - hours) < Tolerance) {
            ShowNotification(Notifications[_Top]);
            _Top++;
        }
    }

    private static void ShowNotification(TimeNotification notification) {
        _Logger.Information("ShowNotification: " + notification.LocomotiveId + " | " + notification.Message);
        NoticeManager.Shared!.PostEphemeral(new EntityReference(EntityType.Car, notification.LocomotiveId), "time", notification.Message);
    }

    public static void FillSortedList(SortedList<string, TimeNotification> notifications) {
        notifications.Clear();
        foreach (var notification in CarInspectorTweaksPlugin.Settings.TimeNotifications) {
            notifications.Add(notification.Identifier, notification);
        }
    }

    public static void SaveSortedList(SortedList<string, TimeNotification> notifications) {
        CarInspectorTweaksPlugin.Settings.TimeNotifications = notifications.Values.ToArray();
        CarInspectorTweaksPlugin.SaveSettings();
        Reset();
    }
}

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("TimeNotifications")]
public class TimeWindowPatch
{
    private static readonly ILogger _Logger = Log.ForContext(typeof(TimeWindowPatch))!;


    private static TimeNotification[] TimeNotifications =>
        CarInspectorTweaksPlugin.Settings.TimeNotificationsEnabled
            ? CarInspectorTweaksPlugin.Settings.TimeNotifications
            : [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TimeWindow), "GetSleepValues")]
    public static bool GetSleepValues(out GameDateTime now, out int targetHour, out float hoursToSleep) {
        var nowLocal = now = TimeWeather.Now;

        _Logger.Information("Now: " + nowLocal.TimeString());

        // find next interchange service time
        GameDateTime? targetTime = null;
        foreach (Interchange? interchange in OpsController.Shared!.EnabledInterchanges!) {

            if (interchange.TryGetExtraScheduled(out var scheduledTime)) {
                _Logger.Information("interchange: " + interchange.DisplayName + " | Extra: " + scheduledTime.TimeString());
            }

            var serviceTime = interchange.GetNextServiceTime(nowLocal, out _);

             _Logger.Information("interchange: " + interchange.DisplayName + " | " + serviceTime.TimeString());

            if (targetTime == null || targetTime > serviceTime) {
                targetTime = serviceTime;
            }
        }

        _Logger.Information("Interchange time: " + targetTime?.TimeString());

        // find next notification time (if any)
        var notificationTime = TimeNotifications.Where(notification => nowLocal.Hours < notification.Hours)
                                                .Select(o => (GameDateTime?)new GameDateTime(nowLocal.Day, o.Hours))
                                                .FirstOrDefault();

        _Logger.Information("Notification: " + notificationTime?.TimeString());


        if (notificationTime != null && (targetTime == null || targetTime > notificationTime)) {
            targetTime = notificationTime.Value;
        }

        _Logger.Information("targetTime: " + targetTime?.TimeString());

        if (targetTime == null) {
            targetHour = 0;
            hoursToSleep = 0f;
            _Logger.Information("GetSleepValues: vanilla");
            return true;
        }

        _Logger.Information("GetSleepValues: custom: " +  targetTime.Value.TimeString());

        // update output values
        targetHour = (int)Math.Floor(targetTime.Value.Hours);

        var secondsToSleep = targetTime.Value.TotalSeconds - nowLocal.TotalSeconds; // Precise difference
        if (secondsToSleep < 0) {
            secondsToSleep += 86400.0; // Handle day wraparound
        }

        hoursToSleep = Convert.ToSingle(secondsToSleep / 3600f);
        _Logger.Information("targetHour: " + targetHour + ", hoursToSleep: " + hoursToSleep);
        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TimeWindow), "<Build>b__21_1", typeof(UIPanelBuilder))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        const string original = "Let time pass until the first scheduled interchange service of the day.";
        const string modded   = "Let time pass until the next scheduled interchange service or custom notification.";

        var code = new List<CodeInstruction>(instructions);

        var replaced = false;
        foreach (var instruction in code) {
            if (instruction.opcode == OpCodes.Ldstr &&
                instruction.operand is original) {
                instruction.operand = modded;
                replaced = true;
            }
        }

        if (!replaced) {
            _Logger.Warning("Tooltip string for Time Window Sleep button not updated.");
        }

        return code.AsEnumerable();
    }
}
