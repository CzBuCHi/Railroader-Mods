using HarmonyLib;
using UnityEngine;
using Model;
using UI.EngineRoster;

namespace Tweaks;

[HarmonyPatch(typeof(EngineRosterRow), "ActionJumpTo")]
internal static class FollowAndSelectPatch
{
    [HarmonyPostfix]
    private static void Postfix(EngineRosterPanel ____parent, BaseLocomotive ____engine)
    {
        if (TweaksPlugin.Settings.FollowAndSelect && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && ____parent != null && ____engine != null)
            ____parent.SelectEngine(____engine, true);
    }
}