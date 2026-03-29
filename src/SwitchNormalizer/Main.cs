using JetBrains.Annotations;
using static UnityModManagerNet.UnityModManager;


namespace SwitchNormalizer
{
    [PublicAPI]
    public static class Main
    {
        private const string PluginIdentifier = "CzBuCHi.SwitchNormalizer";

        private static SwitchNormalizer? _Plugin;

        private static bool Load(ModEntry modEntry) {
            modEntry.OnToggle = OnToggle;

            return true;
        }

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        private static bool OnToggle(ModEntry modEntry, bool value /* to active or deactivate */) {
            if (value) {
                _Plugin = new SwitchNormalizer(modEntry);
            } else {
                _Plugin?.Dispose();
            }

            return true; // If true, the mod will switch the state. If not, the state will not change.
        }
    }
}
