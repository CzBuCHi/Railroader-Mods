using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;

namespace MapEditor.Harmony;

[PublicAPI]
[HarmonyPatch]
public static class PatchEditorPatches
{
    public static void ChangeThing(this PatchEditor __instance, string key, string id, JObject? next, bool onRoot = false) {
        var method = typeof(PatchEditor).GetMethod("ChangeThing", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(__instance, [key, id, next, onRoot]);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PatchEditor), nameof(ChangeThing))]
    public static void ChangeThingPrefix(this PatchEditor __instance, JObject ___root, JObject ___tracksObject, string key, string id, JObject? next, bool onRoot = false) {
        var container = onRoot ? ___root : ___tracksObject;
        container[key] ??= new JObject();
    }
}
