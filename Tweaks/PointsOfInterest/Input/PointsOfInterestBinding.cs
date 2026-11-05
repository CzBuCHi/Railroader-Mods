using HarmonyLib;
using Tweaks.PointsOfInterest;
using UI;
using UnityEngine.InputSystem;

namespace Tweaks.PointsOfInterest.Input
{
    [HarmonyPatch(typeof(GameInput), "Update")]
    internal static class PointsOfInterestBinding
    {
        private static InputAction? action;

        internal static void Enable(Configuration.Settings settings)
        {
            Disable();
            var shortcut = settings.ParseOpenWindowShortcut();
            var name = shortcut.key.ToString();
            var path = "<Keyboard>/" + (name.StartsWith("Digit") ? name.Substring(5) : name.ToLowerInvariant());
            var next = new InputAction("Points of interest", InputActionType.Button);
            try
            {
                if (shortcut.modifiers.Length == 0) next.AddBinding(path);
                else
                {
                    var chord = next.AddCompositeBinding("Modifiers");
                    for (var index = 0; index < shortcut.modifiers.Length; index++)
                        chord.With("modifier" + (index + 1), "<Keyboard>/" + shortcut.modifiers[index]);
                    chord.With("binding", path);
                }
                next.Enable();
                action = next;
            }
            catch { next.Dispose(); throw; }
        }

        internal static void Disable()
        {
            action?.Dispose();
            action = null;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (GameInput.MovementInputEnabled && action != null && action.WasPerformedThisFrame())
                PointsOfInterestFeature.Active?.Runtime.Toggle();
        }
    }
}
