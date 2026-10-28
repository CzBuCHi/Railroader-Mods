using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class InstalledApiTests
{
    internal static void Run(string managed, string modPath)
    {
        using var stream = File.OpenRead(Path.Combine(managed, "Railroader.dll"));
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        Func<string, TypeDefinition> type = name => metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
            .Single(item => metadata.GetString(item.Name) == name);
        Func<string, string, int, bool> method = (owner, name, parameters) => type(owner).GetMethods()
            .Select(handle => metadata.GetMethodDefinition(handle))
            .Any(item => metadata.GetString(item.Name) == name && item.GetParameters()
                .Count(handle => metadata.GetParameter(handle).SequenceNumber != 0) == parameters);
        Program.Check(method("ScriptInterpreterFactory", "CreatePackScopedBuilder", 5), "Harmony target exists in installed game");
        var factory = type("ScriptInterpreterFactory").GetMethods().Select(handle => metadata.GetMethodDefinition(handle))
            .Single(item => metadata.GetString(item.Name) == "CreatePackScopedBuilder");
        var parameters = factory.GetParameters().Select(handle => metadata.GetParameter(handle))
            .Where(item => item.SequenceNumber != 0).Select(item => metadata.GetString(item.Name)).ToArray();
        Program.Check(parameters.Contains("packIdentifier") && parameters.Contains("tier"), "Harmony argument names match installed factory");
        Program.Check(type("RelayUIPanelBuilder").GetFields().Select(handle => metadata.GetFieldDefinition(handle))
            .Any(field => metadata.GetString(field.Name) == "_builder"), "Input adapter backing field exists");
        Program.Check(type("UIPanelBuilder").GetFields().Select(handle => metadata.GetFieldDefinition(handle))
            .Any(field => metadata.GetString(field.Name) == "_container"), "Window clipping container exists");
        Program.Check(method("UIPanelBuilder", "VScrollView", 1), "Native shared scroll box exists");
        Program.Check(method("UIPanelBuilder", "AddInputField", 4), "Native text input overload exists");
        Program.Check(method("UIPanelBuilder", "AddLocationFieldFallback", 2), "Wagon-style location row exists");
        Program.Check(method("UIPanelBuilder", "AddListDetail", 5), "Native purchase-style grouped list exists");
        foreach (var fieldName in new[] { "listController", "detailRectTransform" })
            Program.Check(type("ListDetailController").GetFields().Select(handle => metadata.GetFieldDefinition(handle))
                .Any(field => metadata.GetString(field.Name) == fieldName), "Native list adapter field exists: " + fieldName);
        Program.Check(type("ListController").GetFields().Select(handle => metadata.GetFieldDefinition(handle))
            .Any(field => metadata.GetString(field.Name) == "OnSelectItem"), "Native per-click selection callback exists");
        Program.Check(method("StrategyCameraController", "CaptureState", 0) && method("StrategyCameraController", "Restore", 1), "Native camera state methods exist");
        foreach (var fieldName in new[] { "_movementVelocity", "_distanceVelocity", "_angleXVelocity", "_angleYVelocity" })
            Program.Check(type("StrategyCameraController").GetFields().Select(handle => metadata.GetFieldDefinition(handle))
                .Any(field => metadata.GetString(field.Name) == fieldName), "Camera inertia field exists: " + fieldName);
        Program.Check(method("CameraSelector", "SelectCamera", 1), "Camera mode method exists");
        using var mod = File.OpenRead(modPath);
        using var modPe = new PEReader(mod);
        var modMetadata = modPe.GetMetadataReader();
        var refs = modMetadata.AssemblyReferences.Select(handle => modMetadata.GetString(modMetadata.GetAssemblyReference(handle).Name)).ToArray();
        Program.Check(refs.Contains("Railroader.Modding") && refs.Contains("Relay") && refs.Contains("0Harmony"), "Native contract, Relay, and game's Harmony referenced");
        Program.Check(!refs.Any(name => name.Contains("RailForge") || name.Contains("UnityModManager")), "No legacy loader dependency");
        var entry = modMetadata.TypeDefinitions.Select(handle => modMetadata.GetTypeDefinition(handle))
            .Single(item => modMetadata.GetString(item.Name) == "TeleporterPlugin");
        var interfaces = entry.GetInterfaceImplementations().Select(handle => modMetadata.GetInterfaceImplementation(handle).Interface)
            .Select(handle => modMetadata.GetString(modMetadata.GetTypeReference((TypeReferenceHandle)handle).Name)).ToArray();
        Program.Check(interfaces.Contains("IModEntryPoint") && interfaces.Contains("IWorldLifecycle"), "Native lifecycle interfaces declared");
        Program.Check(modMetadata.ManifestResources.Any(handle => modMetadata.GetString(modMetadata.GetManifestResource(handle).Name) == "Teleporter.icon.png"), "Toolbar icon embedded");
    }
}
