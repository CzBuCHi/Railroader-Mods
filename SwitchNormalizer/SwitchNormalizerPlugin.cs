using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using RailForge;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwitchNormalizer;

public sealed class SwitchNormalizerPlugin : PluginBase
{
    private const string ModIdentifier = "CzBuCHi.SwitchNormalizer";
    private readonly IModdingContext context;
    private Settings settings = new Settings();
    private ToolbarButton? toolbar;
    private bool registered;

    public SwitchNormalizerPlugin(IModdingContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override void OnEnable()
    {
        // RailForge can invoke OnEnable after setting IsEnabled.
        if (registered)
            return;

        settings = context.LoadSettingsData<Settings>(ModIdentifier) ?? new Settings();
        settings.ThrownSwitches = settings.ThrownSwitches ?? Array.Empty<string>();
        var host = new GameObject(ModIdentifier + ".Toolbar");
        Object.DontDestroyOnLoad(host);
        toolbar = host.AddComponent<ToolbarButton>();
        toolbar.Initialize(OnButtonClick);

        Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
        Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapDidUnload);
        registered = true;
    }

    public override void OnDisable()
    {
        if (!registered)
            return;

        Messenger.Default.Unregister(this);
        registered = false;
        if (toolbar != null)
        {
            toolbar.ClearButton();
            Object.Destroy(toolbar.gameObject);
            toolbar = null;
        }
    }

    private void OnMapDidLoad(MapDidLoadEvent message)
    {
        toolbar?.SetMapLoaded(true);
    }

    private void OnMapDidUnload(MapDidUnloadEvent message)
    {
        toolbar?.SetMapLoaded(false);
    }

    private void OnButtonClick()
    {
        try
        {
            var graph = Graph.Shared;
            var controller = TrainController.Shared;
            if (graph == null || controller == null || StateManager.Shared == null)
                return;

            // Resolve the current graph on each click: RailForge may rebuild it.
            var switches = graph.Nodes.Where(node => node != null && graph.IsSwitch(node)).ToArray();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                settings.ThrownSwitches = switches.Where(node => node.isThrown).Select(node => node.id).ToArray();
                context.SaveSettingsData(ModIdentifier, settings);
                Debug.Log("[SwitchNormalizer] Saved switch positions.");
                return;
            }

            var thrown = new HashSet<string>(settings.ThrownSwitches, StringComparer.Ordinal);
            foreach (var node in switches)
            {
                var target = thrown.Contains(node.id);
                if (node.isThrown == target || (node.IsCTCSwitch && !node.IsCTCSwitchUnlocked))
                    continue;
                if (controller.CanSetSwitch(node, target, out _))
                    StateManager.ApplyLocal(new RequestSetSwitch(node.id, target));
            }
        }
        catch (Exception error)
        {
            Debug.LogError("[SwitchNormalizer] Switch action failed: " + error);
        }
    }
}
