using System;
using AlinasMapMod;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapMod.Loaders;

[PublicAPI]
public sealed class LoaderBuilder : ISplineyBuilder
{
    private readonly ILogger _Logger = Log.ForContext<LoaderBuilder>()!;

    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data) {
        var loader = Utility.Deserialize<LoaderData>(data);
        _Logger.Information($"Configuring loader {id} with prefab {loader.Prefab}");
        try {
            loader.Validate();
            return loader.Create(id).gameObject;
        } catch (ValidationException ex) {
            _Logger.Error(ex, "Validation failed for loader {Id}", id);
            throw new ValidationException($"Validation failed for loader {id}: {ex.Message}");
        } catch (Exception ex) {
            _Logger.Error(ex, "Failed to create loader {Id}", id);
            throw new InvalidOperationException($"Failed to create loader {id}", ex);
        }
    }
}
