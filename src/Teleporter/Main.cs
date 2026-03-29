using JetBrains.Annotations;
using static UnityModManagerNet.UnityModManager;


namespace Teleporter
{
    [PublicAPI]
    public static class Main
    {
        private const string PluginIdentifier = "CzBuCHi.Teleporter";

        private static bool Load(ModEntry modEntry) {
            modEntry.OnToggle = OnToggle;
            return true;
        }

        public static Teleporter? Instance { get; private set; }

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        private static bool OnToggle(ModEntry modEntry, bool value /* to active or deactivate */) {
            if (value) {
                Instance = new Teleporter(modEntry);
            } else {
                Instance?.Dispose();
            }

            return true; // If true, the mod will switch the state. If not, the state will not change.
        }
    }
}
