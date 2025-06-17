using System;
using System.Linq;
using System.Reflection;
using Helpers;
using Model.Ops;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapMod.Loaders;

public sealed class Loader : MonoBehaviour
{
    private readonly ILogger _Logger = Log.ForContext<Loader>()!;

    private string   _Prefab   = "";
    private string   _Industry = "";
    private Location _Location = Location.Invalid;
    private bool     _FlipOrientation;

    public string Identifier { get; set; } = "";

    public string Prefab {
        get => _Prefab;
        set {
            _Prefab = value;
            OnPrefabChanged();
        }
    }

    public string Industry {
        get => _Industry;
        set {
            _Industry = value;
            OnIndustryChanged();
        }
    }

    public Location Location {
        get => _Location;
        set {
            _Location = value;
            OnLocationChanged();
        }
    }

    public bool FlipOrientation {
        get => _FlipOrientation;
        set {
            _FlipOrientation = value;
            OnFlipOrientationChanged();
        }
    }

    private void OnPrefabChanged() {
        if (_Prefab == null!) {
            _Logger.Error($"Loader prefab not set for loader: {Identifier}");
            return;
        }

        var prefab = Utility.GameObjectFromUri(_Prefab);
        if (prefab == null) {
            _Logger.Error($"Loader prefab not found: {prefab}");
            throw new ArgumentException("Loader prefab not found " + prefab);
        }

        var oldPrefab = transform.Find("prefab")?.gameObject;
        if (oldPrefab != null) {
            Destroy(oldPrefab);
        }

        var lgo = Instantiate(prefab, transform)!;
        lgo.SetActive(false);
        lgo.name = "prefab";

        UpdatePrefabTransform();

        transform.name = Identifier;

        var trackMarker = lgo.GetComponent<TrackMarker>();
        if (trackMarker != null) {
            trackMarker.enabled = false;
        }

        var globalKeyValueObject = lgo.GetComponent<GlobalKeyValueObject>()!;
        globalKeyValueObject.globalObjectId = Identifier + ".loader";
        foreach (var r in lgo.transform.GetComponentsInChildren<Renderer>()!) {
            r.enabled = true;
        }

        OnIndustryChanged();
        lgo.SetActive(true);
    }

    private void OnIndustryChanged() {
        if (_Industry == "") {
            return;
        }

        try {
            var industry     = FindObjectsByType<Industry>(FindObjectsSortMode.None)!.Single(v => v.identifier == _Industry);
            var targetLoader = GetComponentInChildren<CarLoadTargetLoader>();
            if (targetLoader != null) {
                targetLoader.sourceIndustry = industry;
            }

            var indHover = GetComponentInChildren<IndustryContentHoverable>();
            if (indHover != null) {
                indHover.GetType().GetField("industry", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(indHover, industry);
            }
        } catch (Exception e) {
            _Logger.Error(e, "Failed to find industry {industry} for loader {loader}", _Industry, Identifier);
        }
    }

    private void OnLocationChanged() {
        if (Location == Location.Invalid) {
            return;
        }

        UpdateTransform();
    }

    private void OnFlipOrientationChanged() {
        UpdateTransform();
        UpdatePrefabTransform();
    }

    private void UpdateTransform() {
        var positionRotation = Graph.Shared.GetPositionRotation(Location);
        transform.position = WorldTransformer.GameToWorld(positionRotation.Position);
        transform.rotation = positionRotation.Rotation * Quaternion.Euler(0, 90, 0);
    }

    private void UpdatePrefabTransform() {
        var prefab = transform.Find("prefab")!;

        if (FlipOrientation) {
            switch (_Prefab) {
                case "vanilla://dieselFuelingStand":
                    prefab.transform.localPosition = new Vector3(0, 0, -4.25f);
                    prefab.transform.localRotation = Quaternion.Euler(0, 270, 0);
                    break;
                case "vanilla://coalConveyor":
                    prefab.transform.localPosition = new Vector3(0, 0, -15);
                    prefab.transform.localRotation = Quaternion.identity;
                    break;
                default:
                    prefab.transform.localPosition = Vector3.zero;
                    prefab.transform.localRotation = Quaternion.Euler(0, 180, 0);;
                    break;
            }

        } else {
            switch (_Prefab) {
                case "vanilla://dieselFuelingStand":
                    prefab.transform.localPosition = new Vector3(0, 0, -4.25f);
                    prefab.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    break;
                case "vanilla://coalConveyor":
                    prefab.transform.localPosition = new Vector3(0, 0, -15);
                    prefab.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    break;
                default:
                    prefab.transform.localPosition = Vector3.zero;
                    prefab.transform.localRotation = Quaternion.identity;
                    break;
            }
        }
    }

    public void Rebuild() => OnPrefabChanged();

    public static Loader? FindById(string identifier) => FindObjectsByType<Loader>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        ?.FirstOrDefault(l => l.Identifier == identifier);
}
