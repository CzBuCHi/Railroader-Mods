using System.Linq;
using WaybillMap;

internal static class TenderPairingTests
{
    private sealed class Vehicle
    {
        internal bool NeedsTender, IsTender;
        internal Vehicle? Rear;
    }
    internal static void Run()
    {
        var tender = new Vehicle { IsTender = true };
        var engine = new Vehicle { NeedsTender = true, Rear = tender };
        var pairs = TenderPairing.Find(new[] { tender, engine }, c => c.NeedsTender, c => c.Rear, c => c.IsTender);
        Program.Check(pairs.Count == 1 && pairs[engine] == tender, "Engine owns its connected rear tender regardless of registry order");
        var detached = new Vehicle { IsTender = true };
        Program.Check(!pairs.Values.Contains(detached), "Unconnected tender remains eligible for its own wagon icon");
        engine.Rear = detached;
        pairs = TenderPairing.Find(new[] { engine, tender, detached }, c => c.NeedsTender, c => c.Rear, c => c.IsTender);
        Program.Check(pairs[engine] == detached && !pairs.Values.Contains(tender), "Coupling changes release the previous tender");
        engine.Rear = null;
        Program.Check(TenderPairing.Find(new[] { engine, tender }, c => c.NeedsTender, c => c.Rear, c => c.IsTender).Count == 0,
            "No connected tender produces no merged marker");
        engine.Rear = new Vehicle();
        Program.Check(TenderPairing.Find(new[] { engine }, c => c.NeedsTender, c => c.Rear, c => c.IsTender).Count == 0,
            "Ordinary cars behind an engine are not merged");
        var tank = new Vehicle { Rear = tender };
        Program.Check(TenderPairing.Find(new[] { tank }, c => c.NeedsTender, c => c.Rear, c => c.IsTender).Count == 0,
            "Engines without a tender requirement do not absorb a neighboring tender");
        Program.Check(MarkerGeometry.TryCreate(0.4, 0.5, 0.6, 0.5, 0, 0, 1, 1, -300, -200, 600, 400, out var solo, 10) &&
            MarkerGeometry.TryCreate(0.3, 0.5, 0.6, 0.5, 0, 0, 1, 1, -300, -200, 600, 400, out var merged, 18) &&
            merged.X < solo.X && merged.HalfLength > solo.HalfLength && merged.HalfWidth == solo.HalfWidth,
            "Merged engine+tender marker shifts to combined center and grows in length, not width");
    }
}
