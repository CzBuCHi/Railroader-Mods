using HarmonyLib;
using Model;
using UI;
using UI.EngineRoster;

namespace Tweaks
{
    [HarmonyPatch(typeof(EngineRosterRow), nameof(EngineRosterRow.ActionJumpTo))]
    internal static class FollowAndSelectPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.RosterShiftFollow;

        [HarmonyPostfix]
        private static void Postfix(EngineRosterPanel ____parent, BaseLocomotive ____engine)
        {
            // The original eye action has already started following this engine.
            if (GameInput.IsShiftDown && ____parent != null && ____engine != null)
                ____parent.SelectEngine(____engine, true);
        }
    }
}
