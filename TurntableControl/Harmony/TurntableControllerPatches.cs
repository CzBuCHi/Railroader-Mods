using HarmonyLib;
using JetBrains.Annotations;
using Track;
using TurntableControl.Behaviours;

namespace TurntableControl.Harmony;

[PublicAPI]
[HarmonyPatch]
public class TurntableControllerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TurntableController), "Awake")]
    public static void Awake(TurntableController __instance) {
        __instance.gameObject!.AddComponent<TurntableControllerEx>();
    }
}
