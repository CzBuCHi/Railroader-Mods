using System.Linq;
using HarmonyLib;
using Model;
using Model.Ops;
using UI.Builder;
using UI.CompanyWindow;

namespace Tweaks
{
    [HarmonyPatch(typeof(BuilderExtensions), nameof(BuilderExtensions.AddRepairDestination), typeof(UIPanelBuilder), typeof(Car))]
    internal static class RepairDestinationPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.CopyRepairDestination;

        [HarmonyPostfix]
        private static void Postfix(UIPanelBuilder builder, Car car)
        {
            if (car == null || OpsController.Shared == null ||
                !car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared, out _)) return;
            builder.AddButtonCompact(TextSprites.CopyToCoupled, () =>
            {
                if (car == null || OpsController.Shared == null || !OverrideDestination.Repair.IsWriteAuthorized(car) ||
                    !car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared, out var destination)) return;
                foreach (var target in car.EnumerateCoupled().ToArray())
                    if (target != null && target != car && OverrideDestination.Repair.IsWriteAuthorized(target))
                        target.SetOverrideDestination(OverrideDestination.Repair, destination);
                builder.Rebuild();
            }).Width(28).Height(24).Disable(!OverrideDestination.Repair.IsWriteAuthorized(car))
                .Tooltip("Copy repair destination", "Copy this repair destination to other coupled cars you are authorized to change.");
        }
    }
}
