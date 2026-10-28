using HarmonyLib;
using TMPro;
using UI.Map;
using UnityEngine;

namespace WaybillMap
{
    internal sealed class VehicleMapTag : MonoBehaviour
    {
        internal TMP_Text Text = null!;
        internal void FixRotation()
        {
            // MapEnhancer's FixTextRotation patch, scoped to our own native map icons.
            var angle = Mathf.DeltaAngle(Text.transform.rotation.eulerAngles.y, 180f);
            if (-90f < angle && angle < 90f)
                Text.transform.localRotation = Quaternion.Euler(Text.transform.localRotation.eulerAngles + new Vector3(0, 0, 180));
        }
    }

    [HarmonyPatch(typeof(MapIcon), "FixTextRotation")]
    internal static class VehicleTextRotationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MapIcon __instance)
        {
            var tag = __instance.GetComponent<VehicleMapTag>();
            if (tag == null || tag.Text == null) return true;
            tag.FixRotation(); return false;
        }
    }
}
