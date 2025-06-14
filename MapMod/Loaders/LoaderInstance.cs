using System;
using System.Linq;
using System.Reflection;
using Model.Ops;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapMod.Loaders;

public sealed class LoaderInstance : MonoBehaviour
{
    private readonly ILogger _Logger = Log.ForContext<LoaderInstance>()!;

    public  string Identifier { get; set; } = "";
    private string _Prefab = "";

    public string Prefab {
        get => _Prefab;
        set {
            _Prefab = value;
            OnPrefabChanged();
        }
    }

    private string _Industry = "";

    public string Industry {
        get => _Industry;
        set {
            _Industry = value;
            OnIndustryChanged();
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

    public void Rebuild() => OnPrefabChanged();

    public static LoaderInstance? FindById(string identifier) => FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        ?.FirstOrDefault(l => l.Identifier == identifier);
}
