using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod;
using Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapMod;

public static class Utility
{
    private static JsonSerializerSettings JsonSerializerSettings => new() {
        ContractResolver = new DefaultContractResolver {
            NamingStrategy = new CamelCaseNamingStrategy {
                ProcessDictionaryKeys = false
            }
        },
        Converters = [
            new Vector3Converter()
        ],
        Formatting = Formatting.Indented
    };

    private static JsonSerializer JsonSerializer => JsonSerializer.CreateDefault(JsonSerializerSettings);

    public static T Deserialize<T>(JObject jObject) => jObject.ToObject<T>(JsonSerializer)!;

    public static JObject Serialize<T>(T value) => JObject.FromObject(value!, JsonSerializer);

    private static readonly Dictionary<string, GameObject> _Parents = [];

    public static GameObject GetParent(string id) {
        if (_Parents.TryGetValue(id, out var go)) {
            return go!;
        }

        var world  = GameObject.Find("World")!;
        var parent = new GameObject(id);
        parent.transform.SetParent(world.transform, false);
        _Parents[id] = parent;
        return parent;
    }

    public static GameObject? GameObjectFromUri(string uriString) {
        if (string.IsNullOrEmpty(uriString)) {
            throw new ArgumentException("Invalid uri: empty string is not allowed");
        }

        if (!uriString.Contains("://")) {
            throw new ArgumentException($"Invalid uri: '{uriString}', must match the pattern of (empty|path|scenery|vanilla)://host/path");
        }

        var scheme   = uriString.Split(':')[0];
        var hostPath = uriString.Split(':')[1];
        var segments = hostPath.Split('/');
        var host     = segments[2];
        segments = segments.Skip(3).ToArray();

        switch (scheme) {
            case "path":
                if (host != "scene") {
                    throw new ArgumentException($"Invalid uri: {uriString}, path uris must start with scene");
                }

                var go = GameObject.Find(segments[0])!;
                for (var i = 1; i < segments.Length; i++) {
                    var trans = go.transform.Find(segments[i])!;
                    if (!trans) {
                        Log.Warning("Object not found: {segment}", segments[i]);
                        return null;
                    }

                    go = trans.gameObject;
                }

                return go;

            case "scenery":
                var scenery = Object.FindObjectsOfType<SceneryAssetInstance>(true)!.FirstOrDefault(s => s.name == host);
                if (scenery == null) {
                    Log.Warning("Scenery not found: {host}", host);
                    return null;
                }

                return scenery.gameObject;

            case "vanilla":
                return VanillaPrefabs.GetPrefab(Uri.UnescapeDataString(host));

            case "empty":
                return new GameObject();

            default:
                throw new ArgumentException("Invalid uri or object not found");
        }
    }
}
