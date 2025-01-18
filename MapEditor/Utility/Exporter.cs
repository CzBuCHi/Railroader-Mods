using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Newtonsoft.Json;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor.Utility;

public static class Exporter
{
    public static void Export(GameObject gameObject) {
        Log.Information("Exporting ... " + gameObject.name);

        var data = TraverseHierarchy(gameObject);

        Log.Information("Building json ...");
        var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });
        Log.Information("Building json ... done");
        var path = Path.Combine(MapEditorPlugin.Shared!.Context.ModsBaseDirectory, gameObject.name + ".json");

        File.WriteAllText(path, json, Encoding.UTF8);
        Log.Information("Export saved to " + path);
    }

    public static void ExportWorld() {
        WorldTransformer.TryGetShared(out var worldTransformer);
        Export(worldTransformer!.gameObject);
    }

    public static void ExportScenery() {
        WorldTransformer.TryGetShared(out var worldTransformer);
        var scenery = GetChildByName(worldTransformer!.gameObject, "Large Scenery");

        if (scenery != null) {
            Export(scenery);
        }

        GameObject? GetChildByName(GameObject parent, string name) {
            for (var i = 0; i < parent.transform.childCount; i++) {
                var child = parent.transform.GetChild(i)!;
                if (child.name == name) {
                    return child.gameObject;
                }
            }

            return null;
        }
    }

    private static void FillComponentData(Component component, Type componentType, ComponentData data) {
        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields) {
            object? value;
            try {
                value = PrepareValue(field.GetValue(component));
            } catch (Exception e) {
                value = "Exception: " + e.Message;
            }
            
            data.Fields[field.Name] = value;
        }

        Type[] ignoredProperties = [typeof(Mesh)];
        var readableProperties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(o => o.CanRead && o.GetIndexParameters().Length == 0);

        foreach (var readableProperty in readableProperties) {
            object? value;
            if (ignoredProperties.Contains(readableProperty.PropertyType)) {
                value = "Ignored: " + readableProperty.PropertyType;
            } else {
                try {
                    value = PrepareValue(readableProperty.GetValue(component));
                } catch (Exception e) {
                    value = "Exception: " + e.Message;
                }
            }

            data.Properties[readableProperty.Name] = value;
        }
    }

    private static ComponentData GetComponentData(Component component) {
        var type = component.GetType();

        var compData = new ComponentData {
            TypeName = type.ToString()
        };

        switch (component) {
            case KeyValueObject keyValueObject:
                compData.Properties.Add("RegisteredId", keyValueObject.RegisteredId);
                compData.Properties.Add("Dictionary", keyValueObject.Dictionary!.ToDictionary(o => o.Key, o => PrepareValue(o.Value)));
                break;

            case GlobalKeyValueObject globalKeyValueObject:
                compData.Fields.Add("globalObjectId", globalKeyValueObject.globalObjectId);
                break;

            default:
                FillComponentData(component, type, compData);
                break;
        }

        return compData;
    }

    private static GameObjectData TraverseHierarchy(GameObject obj) {
        var data = new GameObjectData {
            Name = obj.name!,
            IsActive = obj.activeSelf
        };

        foreach (var component in obj.GetComponents<Component>()!) {
            if (component is Transform) {
                continue;
            }

            data.Components.Add(GetComponentData(component));
        }

        if (obj.name is "Track" or "Rivers") {
            data.Children.Add(new GameObjectData { Name = obj.name + " children skipped" });
        } else {
            foreach (Transform child in obj.transform) {
                var childData = TraverseHierarchy(child.gameObject);
                if (childData.Children.Count > 0 || childData.Components.Count > 0) {
                    data.Children.Add(childData);
                }
            }
        }

        return data;
    }

    private static object? PrepareValue(object? value) {
        if (value == null) {
            return null;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string or decimal) {
            return value;
        }

        return value switch {
            Transform transform    => "Transform: " + TransformPath(transform),
            GameObject gameObject  => "GameObject: " + TransformPath(gameObject.transform),
            Vector2 vector2        => $"[{vector2.x}, {vector2.y}]",
            Vector2Int vector2Int  => $"[{vector2Int.x}, {vector2Int.y}]",
            Vector3 vector3        => $"[{vector3.x}, {vector3.y}, {vector3.z}]",
            Vector3Int vector3Int  => $"[{vector3Int.x}, {vector3Int.y}, {vector3Int.z}]",
            Vector4 vector4        => $"[{vector4.x}, {vector4.y}, {vector4.z}, {vector4.w}]",
            Material material      => "Material: " + material.name,
            Bounds bounds          => bounds.ToString(),
            Quaternion quaternion  => quaternion.ToString(),
            Color color            => color.HexString(),
            Delegate               => $"Delegate: {value.GetType().FullName}",
            IEnumerable enumerable => enumerable.Cast<object>().Select(PrepareValue).ToArray(),
            CancellationToken      => "CancellationToken",
            Matrix4x4 matrix       => matrix.ToString(),
            Value keyValue         => keyValue.ToString(),
            Location location      => location + " | " + PrepareValue(location.GetPosition()),
            _                      => $"Type: {value.GetType().FullName}"
        };
    }

    public static string TransformPath(Transform? transform) {
        var path = new List<string>();
        while (transform != null) {
            path.Insert(0, transform.name);
            transform = transform.parent;
        }

        return string.Join(" / ", path);
    }
}

[PublicAPI]
public class ComponentData
{
    public required string                      TypeName   { get; init; }
    public          Dictionary<string, object?> Fields     { get; } = new();
    public          Dictionary<string, object?> Properties { get; } = new();
}

[PublicAPI]
public class GameObjectData
{
    public required string               Name       { get; init; }
    public          bool                 IsActive   { get; init; }
    public          List<ComponentData>  Components { get; } = new();
    public          List<GameObjectData> Children   { get; } = new();
}
