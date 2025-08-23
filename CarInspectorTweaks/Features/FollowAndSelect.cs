using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Utility;
using Model;
using UI.EngineRoster;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("FollowAndSelect")]
public static class FollowAndSelect
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EngineRosterRow), "ActionJumpTo")]
    public static void ActionJumpTo(EngineRosterPanel ____parent, BaseLocomotive ____engine) {
        if (InputHelper.GetShift()) {
            ____parent.SelectEngine(____engine, true);
        }
    }
}
