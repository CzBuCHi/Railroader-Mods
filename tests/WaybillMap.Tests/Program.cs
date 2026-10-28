using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using WaybillMap;

internal static class Program
{
    private static int checks;
    internal static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.00001;

    private static void Main(string[] args)
    {
        Check(Near(MapEnhancerStyle.WagonScaleY(11), 0.8), "MapEnhancer's 11-meter reference retains its 0.8 scale");
        Check(Near(MapEnhancerStyle.WagonScaleY(22), 1.6), "Wagon sprite length doubles for a double-length car");
        Check(Near(MapEnhancerStyle.EngineScaleY(10, 10), 0.8), "Unmerged locomotive retains MapEnhancer's 0.8 scale");
        Check(MapEnhancerStyle.FontSize == 19, "Wagon typography uses the source's 19-unit size");
        Check(MapEnhancerStyle.Label("GTW", "17620", false) == "<line-height=70%>GTW\n17620" &&
            MapEnhancerStyle.Label("ASNR", "2", true) == "2", "Source line spacing and single-line engine/tender labels are retained");
        Check(MapEnhancerStyle.SamplingStride(92, 0.1f) == 1 && MapEnhancerStyle.SamplingStride(92, 0.4f) == 3,
            "Existing overlap filter uses native icon extent and native zoom, without a new gap formula");
        Check(MapVisibility.Show(0, true, false, false, 1, 10) && !MapVisibility.Show(0, false, true, false, 0, 1),
            "Locomotives-only filter excludes cabooses");
        Check(MapVisibility.Show(1, false, true, false, 1, 10) && !MapVisibility.Show(1, false, false, true, 0, 1),
            "Caboose filter excludes ordinary cars even when selected");
        Check(Enumerable.Range(0, 9).Where(i => MapVisibility.Show(2, false, false, false, i, 3)).SequenceEqual(new[] { 0, 3, 6 }),
            "Overlap sampling retains stable consist indices");
        Check(MapVisibility.Show(2, false, false, true, 1, 10) && MapVisibility.Show(2, false, true, false, 1, 10),
            "Selected cars and cabooses survive ordinary-car sampling");
        Check(Near(MarkerStyle.DestinationChannel(0.8f, true), 0.68) && MarkerStyle.DestinationChannel(0.8f, false) == 0.8f,
            "Requested mild spotted dimming is retained over the source rendering port");
        Check(MarkerStyle.UseDarkText(221 / 255d, 221 / 255d, 221 / 255d, 1) && !MarkerStyle.UseDarkText(0.05, 0.05, 0.3, 1),
            "Light and dark waybill backgrounds retain contrasting text");
        SwitchProfileTests.Run(); TenderPairingTests.Run();
        Inspect(args[0], args[1]);
        Console.WriteLine("PASS: " + checks + " native-rendering, filter, profile and installed-API checks.");
    }

    private static void Inspect(string managed, string modPath)
    {
        using var gameStream = File.OpenRead(Path.Combine(managed, "Railroader.dll"));
        using var gamePe = new PEReader(gameStream); var game = gamePe.GetMetadataReader();
        TypeDefinition Type(string ns, string name) => game.TypeDefinitions.Select(game.GetTypeDefinition)
            .Single(t => game.GetString(t.Name) == name && game.GetString(t.Namespace) == ns);
        bool Method(TypeDefinition type, string name, int arity) => type.GetMethods().Select(game.GetMethodDefinition)
            .Any(m => game.GetString(m.Name) == name && m.GetParameters().Select(game.GetParameter).Count(p => p.SequenceNumber > 0) == arity);
        var windowFields = Type("UI.Map", "MapWindow").GetFields().Select(game.GetFieldDefinition).Select(f => game.GetString(f.Name)).ToArray();
        Check(Method(Type("UI.Map", "MapWindow"), "Start", 0) && windowFields.Contains("mapBuilder") && windowFields.Contains("rawImage"),
            "Map installation targets match installed game");
        Check(Method(Type("UI.Map", "MapDrag"), "OnDrag", 1), "Native mouse-drag prefix target exists");
        Check(Method(Type("UI.Map", "MapIcon"), "SetZoom", 1) && Method(Type("UI.Map", "MapIcon"), "FixTextRotation", 0) &&
            Method(Type("UI.Map", "MapBuilder"), "get_IconScale", 0), "Native map-icon zoom and scoped text-rotation targets exist");
        var carFields = Type("Model", "Car").GetFields().Select(game.GetFieldDefinition).Select(f => game.GetString(f.Name)).ToArray();
        Check(carFields.Contains("_setupPrefabs") && carFields.Contains("MapIcon"), "Native prefab access and original-visibility fields match");
        Check(Method(Type("Model.Ops", "OpsControllerExtensions"), "TryGetDestinationInfo", 6), "Destination lookup includes repair overrides");
        Check(Method(Type("UI.Map", "MapDisplayOptions"), "Add", 1), "Map display extension point exists");
        using var modStream = File.OpenRead(modPath); using var modPe = new PEReader(modStream); var mod = modPe.GetMetadataReader();
        var refs = mod.AssemblyReferences.Select(mod.GetAssemblyReference).Select(r => mod.GetString(r.Name)).ToArray();
        Check(refs.Contains("Railroader.Modding") && refs.Contains("0Harmony") && !refs.Any(n => n.Contains("MapEnhancer") || n.Contains("RailForge")),
            "Port uses native loader and has no legacy MapEnhancer DLL dependency");
        var entry = mod.TypeDefinitions.Select(mod.GetTypeDefinition).Single(t => mod.GetString(t.Name) == "WaybillMapPlugin");
        var interfaces = entry.GetInterfaceImplementations().Select(mod.GetInterfaceImplementation)
            .Select(i => mod.GetString(mod.GetTypeReference((TypeReferenceHandle)i.Interface).Name)).ToArray();
        Check(interfaces.Contains("IModEntryPoint") && interfaces.Contains("IWorldLifecycle"), "Native lifecycle and cleanup interfaces present");
        var types = mod.TypeDefinitions.Select(mod.GetTypeDefinition).Select(t => mod.GetString(t.Name)).ToArray();
        Check(types.Contains("VehicleMapTag") && types.Contains("VehicleTextRotationPatch") && !types.Contains("VehicleLayer") && !types.Contains("MarkerLayout"),
            "Native marker port replaces the previous UI geometry and spacing heuristics");
        Check(types.Contains("StopFollowingOnMapDrag"), "Dragging disables follow through an installed Harmony prefix");
        var members = mod.MemberReferences.Select(mod.GetMemberReference).Select(m => mod.GetString(m.Name)).ToArray();
        Check(members.Contains("Instantiate") && members.Contains("SetZoom") && members.Contains("SetPositionAndRotation") && members.Contains("GetCenterRotation"),
            "Rendering uses real native map icons and source-style world transforms");
        Check(members.Contains("set_autoSizeTextContainer") && members.Contains("set_fontSize") && members.Contains("set_enableAutoSizing"),
            "Copied MapEnhancer typography settings are applied to real native text components");
        Check(members.Contains("TryGetDestinationInfo") && members.Contains("AreaForCarPosition") && members.Contains("SetMapCenter"),
            "Destination color and selected-car following APIs are retained");
        Check(members.Contains("CanSetSwitch") && members.Contains("CheckAuthorizedToSendMessage") && members.Contains("ApplyLocal"),
            "Switch requests retain occupancy and authorization guards");
        Check(!members.Any(n => new[] { "SetOverrideDestination", "SetHandbrake", "SetGladhandsConnected" }.Contains(n)), "Rendering does not mutate car state");
        var resource = mod.ManifestResources.Select(mod.GetManifestResource).Single(r => mod.GetString(r.Name) == "WaybillMap.traincar.png");
        var reader = modPe.GetSectionData(modPe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress + (int)resource.Offset).GetReader();
        var png = reader.ReadBytes(reader.ReadInt32());
        Check(Convert.ToHexString(SHA256.HashData(png)) == "BEEBBBDF720794303280EACCB4FB2E08095921C36677820A5626D041ECD7F768",
            "Embedded wagon sprite is byte-for-byte identical to the provided MapEnhancer asset");
    }
}
