using System;
using System.Collections.Generic;
using Game;
using Game.Messages;
using Game.State;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UI;
using UI.Builder;
using UIFramework.Models;
using UIFramework.Transform;

namespace UIFramework.Patches
{
    [PublicAPI]
    [HarmonyPatch]
    public static class TimeWindowPatches
    {
        /// <summary>
        /// Metadata cache. We only need to build the metadata once, and it is expensive to build, so we cache it here. It is built on first opening the time window.
        /// </summary>
        private static UIPanelMetadata? _Metadata;

        /// <summary>
        /// This code replaces original render with a custom one built from metadata.
        /// This allows us to add new UI elements and update them without needing to patch every single method that updates the time window.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TimeWindow), "Build")]
        private static bool Build(UIPanelBuilder builder) {
            _Metadata ??= UIPanelTransformRegistry.Transform(BuildMetadata());
            UIPanelGenerator.Build(builder, _Metadata);
            return false;
        }

        /// <summary>
        /// Builds the metadata for the time window UI. This represents original UI elements.
        /// </summary>
        /// <returns></returns>
        private static UIPanelMetadata BuildMetadata() {
            var elements = new List<UIPanelElement> {
                new DynamicLabel(TimeWindow.Shared.GetTimeString, UIPanelBuilder.Frequency.Fast) {
                    HorizontalTextAlignment = HorizontalAlignmentOptions.Center
                }
            };

            if (StateManager.CheckAuthorizedToSendMessage(new WaitTime())) {
                elements.Add(new Section("Pass Time"));
                elements.Add(new Label("<style=Footnote>Does not affect train movement."));
                elements.Add(new HStack(
                    new Button("Wait 5 min", () => Wait(0.0833333358f)),
                    new Button("15 min", () => Wait(0.25f)),
                    new Button("1 hr", () => Wait(1f)),
                    new Button("6 hr", () => Wait(6f))
                ));
                elements.Add(new HStack(
                    new Button("Sleep", () => {
                        TimeWindow.Shared.GetSleepValues(out _, out _, out var hoursToSleep);
                        Wait(hoursToSleep);
                    }) {
                        ToolTip = new ToolTip("Sleep", "Let time pass until the first scheduled interchange service of the day.")
                    },
                    new DynamicLabel((Func<string>)(() => {
                        TimeWindow.Shared.GetSleepValues(out var now, out _, out var hoursToSleep);
                        var self = now.AddingHours(hoursToSleep);
                        return $"{self.IntervalString(now, GameDateTimeInterval.Style.Short)} until {self.TimeString()}";
                    }), UIPanelBuilder.Frequency.Periodic) {
                        VerticalTextAlignment = VerticalAlignmentOptions.Middle
                    }
                ));
            }

            elements.Add(new ExpandingVerticalSpacer());
            return new UIPanelMetadata(elements) {
                Spacing = 8f
            };
        }

        // private methods accessors

        #region GetTimeString

        private delegate string GetTimeStringDelegate(TimeWindow instance);

        private static readonly GetTimeStringDelegate _GetTimeString = AccessTools.MethodDelegate<GetTimeStringDelegate>(AccessTools.Method(typeof(TimeWindow), "GetTimeString")!)!;

        public static string GetTimeString(this TimeWindow t) => _GetTimeString(t);

        #endregion

        #region Wait

        private delegate void WaitDelegate(float hours);

        private static readonly WaitDelegate _Wait = AccessTools.MethodDelegate<WaitDelegate>(AccessTools.Method(typeof(TimeWindow), "Wait")!)!;

        public static void Wait(float hours) => _Wait(hours);

        #endregion

        #region GetSleepValues

        private delegate void GetSleepValuesDelegate(TimeWindow instance, out GameDateTime now, out int targetHour, out float hoursToSleep);

        private static readonly GetSleepValuesDelegate _GetSleepValues = AccessTools.MethodDelegate<GetSleepValuesDelegate>(AccessTools.Method(typeof(TimeWindow), "GetSleepValues")!)!;

        public static void GetSleepValues(this TimeWindow t, out GameDateTime now, out int targetHour, out float hoursToSleep) => _GetSleepValues(t, out now, out targetHour, out hoursToSleep);

        #endregion
    }
}
