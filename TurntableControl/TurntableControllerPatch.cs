using HarmonyLib;
using Track;

namespace TurntableControl;

[HarmonyPatch(typeof(TurntableController), "FixedUpdate")]
internal static class TurntableControllerPatch
{
    [HarmonyPrefix]
    private static void Prefix(TurntableController __instance, float ____speed)
    {
        __instance.GetComponent<TurntableDriver>()?.BeforeNativeStep(____speed);
    }

    [HarmonyPostfix]
    private static void Postfix(TurntableController __instance, ref float ____speed, ref bool ____wasMoving)
    {
        __instance.GetComponent<TurntableDriver>()?.AfterNativeStep(ref ____speed, ref ____wasMoving);
    }
}
