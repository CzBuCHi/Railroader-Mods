using JetBrains.Annotations;
using UnityModManagerNet;

namespace TurntableControl
{
    [PublicAPI]
    public static class Main
    {
        private const string PluginIdentifier = "CzBuCHi.TurntableControl";

        private static bool Load(UnityModManager.ModEntry modEntry) {
            modEntry.OnToggle = OnToggle;
            return true;
        }

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* to active or deactivate */) {
            if (value) {
                var harmony = new HarmonyLib.Harmony(PluginIdentifier);
                harmony.PatchAll();
            } else {
                var harmony = new HarmonyLib.Harmony(PluginIdentifier);
                harmony.UnpatchAll();
            }

            return true; // If true, the mod will switch the state. If not, the state will not change.
        }
    }
}
