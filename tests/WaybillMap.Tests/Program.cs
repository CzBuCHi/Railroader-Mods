using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using WaybillMap;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;
    private static bool Create(double ax, double ay, double bx, double by, out MarkerGeometry marker) =>
        MarkerGeometry.TryCreate(ax, ay, bx, by, 0, 0, 1, 1, -300, -200, 600, 400, out marker);

    private static void Main(string[] args)
    {
        Check(Create(0.4, 0.5, 0.6, 0.5, out var horizontal) && Near(horizontal.X, 0) && Near(horizontal.Y, 0) &&
            Near(horizontal.AxisX, 1) && Near(horizontal.AxisY, 0), "Horizontal car projects to centered rectangle");
        Check(Near(horizontal.HalfLength, 50.4) && Near(horizontal.HalfWidth, 3.5), "Length follows projected body and width is bounded");
        Check(Create(0.5, 0.4, 0.5, 0.6, out var vertical) && Near(vertical.AxisX, 0) && Near(vertical.AxisY, 1) &&
            Near(vertical.HalfLength, 33.6), "Vertical car respects map aspect ratio");
        Check(Create(0.4, 0.3, 0.6, 0.7, out var diagonal) && Near(diagonal.AxisX * diagonal.AxisX + diagonal.AxisY * diagonal.AxisY, 1),
            "Diagonal marker direction is normalized");
        Check(Create(0.6, 0.5, 0.4, 0.5, out var reversed) && Near(reversed.X, horizontal.X) && Near(reversed.HalfLength, horizontal.HalfLength) &&
            Near(reversed.AxisX, -horizontal.AxisX), "Reversing logical ends retains marker footprint");
        Check(Create(0.5, 0.5, 0.5, 0.5, out var tiny) && Near(tiny.HalfLength, 2.5) && Near(tiny.HalfWidth, 1.25),
            "Cars remain visible at distant zoom without division by zero");
        Check(!Create(2, 0.5, 2.01, 0.5, out _), "Entirely offscreen car is culled");
        Check(Create(0.999, 0.5, 1.02, 0.5, out _), "Partially visible edge car is retained for masking");
        Check(MarkerGeometry.TryCreate(0.3, 0.3, 0.7, 0.3, 0.2, 0.1, 0.6, 0.4, -300, -200, 600, 400, out var cropped) &&
            Near(cropped.X, 0) && Near(cropped.Y, 0), "Cropped UV rectangle maps positions into displayed area");
        Check(MarkerGeometry.TryCreate(0.5, 0.25, 0.5, 0.25, 0, 1, 1, -1, -300, -200, 600, 400, out var flipped) &&
            Near(flipped.Y, 100), "Flipped render-texture UVs are respected");
        Check(MarkerGeometry.TryCreate(0.5, 0.5, 0.6, 0.5, 0, 0, 1, 1, 0, 0, 1200, 800, out var resized) &&
            Near(resized.X, 660) && Near(resized.Y, 400), "Resize and non-centered pivot coordinates are respected");
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Check(!Create(invalid, 0.5, 0.6, 0.5, out _), "Nonfinite projection is rejected");
        Check(!MarkerGeometry.TryCreate(0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 600, 400, out _) &&
            !MarkerGeometry.TryCreate(0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 400, out _), "Zero-size UV/image cannot divide by zero");
        Check(MarkerGeometry.BatchSize * 8 < 65535, "Each outlined-marker batch stays below Unity UI vertex limit");
        Inspect(args[0], args[1]);
        Console.WriteLine("PASS: " + checks + " map geometry and installed API/compiled-mod checks.");
    }

    private static void Inspect(string managed, string modPath)
    {
        using var gameStream = File.OpenRead(Path.Combine(managed, "Railroader.dll"));
        using var gamePe = new PEReader(gameStream);
        var game = gamePe.GetMetadataReader();
        TypeDefinition Type(string ns, string name) => game.TypeDefinitions.Select(game.GetTypeDefinition)
            .Single(t => game.GetString(t.Name) == name && game.GetString(t.Namespace) == ns);
        bool Method(TypeDefinition type, string name, int arity) => type.GetMethods().Select(game.GetMethodDefinition)
            .Any(m => game.GetString(m.Name) == name && m.GetParameters().Select(game.GetParameter).Count(p => p.SequenceNumber > 0) == arity);
        var window = Type("UI.Map", "MapWindow");
        Check(Method(window, "Start", 0), "Map window lifecycle patch target exists");
        var fields = window.GetFields().Select(game.GetFieldDefinition).Select(f => game.GetString(f.Name)).ToArray();
        Check(fields.Contains("rawImage") && fields.Contains("mapBuilder"), "Native map image and camera builder field names match");
        Check(Method(Type("UI.Map", "MapDisplayOptions"), "Add", 1) &&
            Method(Type("UI.Map", "MapDisplayOption"), ".ctor", 3), "Native map display extension registry exists");
        Check(Method(Type("", "TrainController"), "get_Cars", 0), "Global logical car registry exists");
        Check(Method(Type("UI.Tags", "CarTagColor"), "For", 2), "Native destination tag color API exists");
        Check(Method(Type("Helpers", "WorldTransformer"), "GameToWorld", 1), "Floating world-origin conversion API exists");
        Check(Method(Type("UI.Windows", "Window"), "get_IsShown", 0), "Native map visibility API exists");

        using var modStream = File.OpenRead(modPath);
        using var modPe = new PEReader(modStream);
        var mod = modPe.GetMetadataReader();
        var refs = mod.AssemblyReferences.Select(mod.GetAssemblyReference).Select(r => mod.GetString(r.Name)).ToArray();
        Check(refs.Contains("Railroader.Modding") && refs.Contains("0Harmony") && !refs.Any(n => n.Contains("Tweaks") || n.Contains("RailForge")),
            "Mod uses native loader/Harmony and is independent of Tweaks and RailForge");
        var entry = mod.TypeDefinitions.Select(mod.GetTypeDefinition).Single(t => mod.GetString(t.Name) == "WaybillMapPlugin");
        var interfaces = entry.GetInterfaceImplementations().Select(mod.GetInterfaceImplementation)
            .Select(i => mod.GetString(mod.GetTypeReference((TypeReferenceHandle)i.Interface).Name)).ToArray();
        Check(interfaces.Contains("IModEntryPoint") && interfaces.Contains("IWorldLifecycle"), "Native load/world cleanup hooks are implemented");
        var members = mod.MemberReferences.Select(mod.GetMemberReference).Select(m => mod.GetString(m.Name)).ToArray();
        Check(members.Contains("get_Cars") && members.Contains("WorldToViewportPoint") && members.Contains("GameToWorld") && members.Contains("get_IsOn"),
            "Overlay reads logical car registry, projects camera positions and respects display toggle");
        Check(mod.TypeReferences.Select(mod.GetTypeReference).Any(t => mod.GetString(t.Name) == "CarTagColor"), "Compiled overlay uses native waybill/tag color rules");
        Check(mod.TypeReferences.Select(mod.GetTypeReference).Any(t => mod.GetString(t.Name) == "RectMask2D") && members.Contains("set_raycastTarget"),
            "Overlay uses clipping and does not intercept map input");
        Check(!members.Any(n => new[] { "ApplyLocal", "Send", "SetOverrideDestination", "SetHandbrake", "SetGladhandsConnected" }.Contains(n)),
            "Display mod has no game-state mutation APIs");
    }
}
