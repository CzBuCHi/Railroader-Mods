using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Tweaks;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
    }
    private sealed class WarningCar
    {
        internal bool Enabled = true;
        internal bool Hotbox;
        internal float Oil;
    }
    private static WarningCar? Warning(params WarningCar[] cars) =>
        HudRules.FindWarning(cars, car => car.Enabled, car => car.Hotbox, car => car.Oil);

    private static void Main(string[] args)
    {
        Check(NextServiceSelection.Find(100, Array.Empty<double>()) == null, "No interchange falls back to native sleep");
        Check(NextServiceSelection.Find(100, new[] { 200d, 150d }) == 150, "Earliest enabled service wins");
        Check(NextServiceSelection.Find(100, new[] { 500d, 200d, 350d }) == 200, "Extra service before daily service wins");
        Check(NextServiceSelection.Find(100, new[] { 101d, 400d }) == 101, "One-second future service is retained");
        Check(NextServiceSelection.Find(100, new[] { 50d, 200d }) == 101, "Overdue service advances one second");
        Check(NextServiceSelection.Find(100, new[] { 100d }) == 101, "Due-now service advances one second");
        Check(NextServiceSelection.Find(86300, new[] { 90000d, 86410d }) == 86410, "Services across midnight use absolute time");
        Check(NextServiceSelection.Find(100, new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }) == null, "Invalid service times ignored");
        Check(NextServiceSelection.Find(100, new[] { double.NaN, 300d }) == 300, "A valid service survives invalid alternatives");
        for (var mode = 0; mode < 3; mode++) Check(HudRules.HighlightedMode(mode) == mode, "Manual/Road/Yard highlight their matching button");
        Check(HudRules.HighlightedMode(3) == -1, "AE Waypoint highlights no button");
        Check(HudRules.HighlightedMode(-1) == -1 && HudRules.HighlightedMode(99) == -1, "Unknown modes do not select a button");
        Check(!HudRules.NeedsAir(false, true, false, 0), "No car set means no air repair action");
        Check(HudRules.NeedsAir(true, true, false, 1), "Coupled disconnected gladhands need repair");
        Check(HudRules.NeedsAir(true, true, true, 0), "Coupled closed anglecock needs opening");
        Check(HudRules.NeedsAir(true, true, true, 0.5f), "Partially open internal anglecock needs opening");
        Check(!HudRules.NeedsAir(true, true, true, 1), "Healthy coupled end needs no action");
        Check(HudRules.NeedsAir(true, false, false, 1), "Outer open anglecock needs closing");
        Check(!HudRules.NeedsAir(true, false, false, 0), "Outer closed anglecock is correct");
        var low = new WarningCar { Oil = 0.1f };
        var medium = new WarningCar { Oil = 0.4f };
        var threshold = new WarningCar { Oil = 0.5f };
        var hot = new WarningCar { Oil = 1, Hotbox = true };
        var disabled = new WarningCar { Oil = 0, Hotbox = true, Enabled = false };
        Check(Warning() == null, "No consist means no oil warning");
        Check(Warning(threshold) == null, "Oil at threshold produces no warning");
        Check(Warning(medium, low) == low, "Lowest oil below threshold wins");
        Check(Warning(low, hot) == hot, "Hotbox takes priority over low oil even at 100 percent oil");
        Check(Warning(disabled) == null, "Disabled lubrication suppresses oil/hotbox button");
        Check(Warning(disabled, medium) == medium, "Unsupported/disabled cars do not hide a real oil warning");
        InspectInstalledGame(args[0], args[1]);
        Console.WriteLine("PASS: " + checks + " timing, HUD policy, installed API and compiled-mod checks.");
    }

    private static void InspectInstalledGame(string managed, string modPath)
    {
        using var stream = File.OpenRead(Path.Combine(managed, "Railroader.dll"));
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        Func<string, TypeDefinition> type = name => metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
            .Single(item => item.GetDeclaringType().IsNil && metadata.GetString(item.Name) == name &&
                (name != "Car" || metadata.GetString(item.Namespace) == "Model"));
        Func<string, string, int, bool> method = (owner, name, count) => type(owner).GetMethods()
            .Select(handle => metadata.GetMethodDefinition(handle))
            .Any(item => metadata.GetString(item.Name) == name && item.GetParameters()
                .Count(handle => metadata.GetParameter(handle).SequenceNumber > 0) == count);
        Check(method("TimeWindow", "GetSleepValues", 3), "Native sleep patch target exists");
        Check(method("Interchange", "GetNextServiceTime", 3), "Interchange daily/extra-aware service API exists");
        Check(method("CarInspector", "PopulateCarPanel", 1), "Native car inspector patch target exists");
        Check(method("LocomotiveControlsUIAdapter", "OnEnable", 0) && method("LocomotiveControlsUIAdapter", "DropdownDidChange", 1), "HUD patch target and native mode handler exist");
        var adapterFields = type("LocomotiveControlsUIAdapter").GetFields().Select(handle => metadata.GetString(metadata.GetFieldDefinition(handle).Name)).ToArray();
        Check(adapterFields.Contains("modeDropdown") && adapterFields.Contains("speedLabel"), "HUD field names match injection targets");
        Check(type("WindowManager").GetFields().Any(handle => metadata.GetString(metadata.GetFieldDefinition(handle).Name) == "builderAssets"), "Current UI button assets exist");
        Check(method("Car", "get_EnableOiling", 0) && method("Car", "get_Oiled", 0) && method("Car", "get_HasHotbox", 0), "Native oil feature/car support and warning APIs exist");
        Check(method("CameraSelector", "ZoomToPoint", 1) && method("CameraSelector", "ZoomToCar", 2), "Native camera navigation methods exist");
        Check(method("Car", "get_LocationA", 0) && method("Car", "get_LocationB", 0), "Logical coupler-end positions exist");
        var tooltipTargets = 0;
        foreach (var handle in type("TimeWindow").GetMethods())
        {
            var item = metadata.GetMethodDefinition(handle);
            if (item.RelativeVirtualAddress == 0) continue;
            var bytes = pe.GetMethodBody(item.RelativeVirtualAddress).GetILBytes()!;
            for (var i = 0; i + 4 < bytes.Length; i++)
            {
                if (bytes[i] != 0x72) continue;
                try
                {
                    var token = BitConverter.ToInt32(bytes, i + 1);
                    if ((token & unchecked((int)0xff000000)) != 0x70000000) continue;
                    if (metadata.GetUserString(MetadataTokens.UserStringHandle(token & 0x00ffffff)) ==
                        "Let time pass until the first scheduled interchange service of the day.") { tooltipTargets++; break; }
                }
                catch (BadImageFormatException) { }
                catch (ArgumentException) { }
            }
        }
        Check(tooltipTargets == 1, "Exactly one installed sleep tooltip literal is patched");

        using var mod = File.OpenRead(modPath);
        using var modPe = new PEReader(mod);
        var modMetadata = modPe.GetMetadataReader();
        var refs = modMetadata.AssemblyReferences.Select(handle => modMetadata.GetString(modMetadata.GetAssemblyReference(handle).Name)).ToArray();
        Check(refs.Contains("Railroader.Modding") && refs.Contains("0Harmony"), "Native loader and game's Harmony referenced");
        Check(!refs.Any(name => name.Contains("RailForge") || name.Contains("UnityModManager")), "No legacy loader dependency");
        var entry = modMetadata.TypeDefinitions.Select(handle => modMetadata.GetTypeDefinition(handle)).Single(item => modMetadata.GetString(item.Name) == "TweaksPlugin");
        var interfaces = entry.GetInterfaceImplementations().Select(handle => modMetadata.GetInterfaceImplementation(handle).Interface)
            .Select(handle => modMetadata.GetString(modMetadata.GetTypeReference((TypeReferenceHandle)handle).Name)).ToArray();
        Check(interfaces.Contains("IModEntryPoint") && interfaces.Contains("IWorldLifecycle"), "Native lifecycle hooks present");
        var names = modMetadata.TypeDefinitions.Select(handle => modMetadata.GetString(modMetadata.GetTypeDefinition(handle).Name)).ToArray();
        Check(!names.Any(name => new[] { "Autosave", "RepairSounds", "Prank", "CopyCrew", "BleedAll", "WaypointShortcut", "TrainLength" }.Any(name.Contains)), "Unrequested legacy features are absent from the compiled mod");
        Check(names.Contains("ControlModeButtons") && names.Contains("ConsistActions") && names.Contains("CarPanelPatch") && names.Contains("SleepUntilInterchangePatch"), "All four requested feature groups compiled");
        var members = modMetadata.MemberReferences.Select(handle => modMetadata.GetString(modMetadata.GetMemberReference(handle).Name)).ToArray();
        Check(!members.Any(name => new[] { "SetHandbrake", "ApplyEndGearChange", "set_AnglecockSetting", "ApplyLocal", "SetGladhandsConnected" }.Contains(name)),
            "HUD actions have no car/brake/air mutation calls or direct state-message writes");
        Check(members.Contains("ZoomToPoint") && members.Contains("ZoomToCar"), "HUD navigation targets camera APIs");
    }
}
