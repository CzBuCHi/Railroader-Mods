using System;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using RailForge;
using UI.Builder;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Teleporter;

public sealed class TeleporterPlugin : PluginBase, IModTabHandler
{
    internal const string Identifier = "CzBuCHi.Teleporter";
    private readonly IModdingContext context;
    private TeleporterRuntime? runtime;
    internal Settings Settings { get; private set; } = new Settings();

    public TeleporterPlugin(IModdingContext context) { this.context = context; }

    public override void OnEnable()
    {
        if (runtime != null)
            return;
        Settings = context.LoadSettingsData<Settings>(Identifier) ?? new Settings();
        Settings.Groups = Settings.Groups ?? new System.Collections.Generic.List<LocationGroup>();
        Settings.Groups.RemoveAll(group => group == null);
        foreach (var group in Settings.Groups)
            group.Locations = group.Locations ?? new System.Collections.Generic.Dictionary<string, TeleportLocation>();
        var host = new GameObject(Identifier + ".Runtime");
        Object.DontDestroyOnLoad(host);
        runtime = host.AddComponent<TeleporterRuntime>();
        runtime.Initialize(this);
        Messenger.Default.Register<MapDidLoadEvent>(this, OnMapLoaded);
        Messenger.Default.Register<MapWillUnloadEvent>(this, OnMapWillUnload);
        Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapUnloaded);
    }

    public override void OnDisable()
    {
        Messenger.Default.Unregister(this);
        if (runtime != null)
        {
            runtime.SetMapLoaded(false);
            Object.Destroy(runtime.gameObject);
            runtime = null;
        }
    }

    private void OnMapLoaded(MapDidLoadEvent message) => runtime?.SetMapLoaded(true);
    private void OnMapWillUnload(MapWillUnloadEvent message) => runtime?.SetMapLoaded(false);
    private void OnMapUnloaded(MapDidUnloadEvent message) => runtime?.SetMapLoaded(false);

    internal void Save() => context.SaveSettingsData(Identifier, Settings);

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        builder.AddFieldToggle("Open Teleporter on map load", () => Settings.AutoOpenTeleporterWindow,
            value => { Settings.AutoOpenTeleporterWindow = value; Save(); });
    }
    public void ModTabDidClose() => Save();
}