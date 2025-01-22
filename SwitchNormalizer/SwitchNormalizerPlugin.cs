using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Utility;
using Railloader;
using Track;
using UnityEngine;

namespace SwitchNormalizer;

[PublicAPI]
public sealed class SwitchNormalizerPlugin(IModdingContext context, IUIHelper uiHelper) : SingletonPluginBase<SwitchNormalizerPlugin>
{
    private const string ModIdentifier = "CzBuCHi.SwitchNormalizer";

    private Messenger   _Messenger = null!;
    private Settings    _Settings  = null!;
    private TrackNode[] _Switches  = null!;

    public override void OnEnable() {
        _Messenger = Messenger.Default!;
        _Messenger.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
        _Messenger.Register(this, new Action<MapDidUnloadEvent>(OnMapDidUnload));
    }

    public override void OnDisable() => _Messenger.Unregister(this);

    private void OnMapDidLoad(MapDidLoadEvent @event) {
        CzBuCHi.Shared.UI.TopRightArea.AddButton("SwitchNormalizer.icon.png", "Switch Normalizer", 9, OnButtonClick);
        
        _Settings = context.LoadSettingsData<Settings>(ModIdentifier) ?? new Settings();
        _Switches = Graph.Shared!.Nodes!.Where(Graph.Shared.IsSwitch).ToArray();
    }

    private void OnMapDidUnload(MapDidUnloadEvent obj) {
        context.SaveSettingsData(ModIdentifier, _Settings);
    }

    private void OnButtonClick() {
        if (InputHelper.GetShift()) {
            _Settings.ThrownSwitches = _Switches.Where(o => o.isThrown).Select(o => o.id).ToArray();
        } else {
            _Switches.Do(node => node.isThrown = _Settings.ThrownSwitches.Contains(node.id));
        }
    }
}
