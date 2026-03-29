using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Track;
using UIFramework.Tools;
using UnityEngine;
using UnityModManagerNet;

namespace SwitchNormalizer
{
    public sealed class SwitchNormalizer : IDisposable
    {
        private readonly UnityModManager.ModEntry _ModEntry;
        private readonly Settings                 _Settings;
        private          TrackNode[]              _Switches = null!;

        public SwitchNormalizer(UnityModManager.ModEntry modEntry) {
            _ModEntry = modEntry;

            Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));

            _Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
        }

        public void Dispose() {
            Messenger.Default.Unregister(this);
        }

        private void OnMapDidLoad(MapDidLoadEvent @event) {
            TopRightAreaHelper.AddButton(typeof(Main).Assembly, "SwitchNormalizer.icon.png", "Switch Normalizer", 9, OnButtonClick);
            _Switches = Graph.Shared.Nodes.Where(Graph.Shared.IsSwitch).ToArray();
        }

        private void OnButtonClick() {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                _Settings.ThrownSwitches = _Switches.Where(o => o.isThrown).Select(o => o.id).ToArray();
                _Settings.Save(_ModEntry);
            } else {
                _Switches.Do(node => {
                    var isThrown = _Settings.ThrownSwitches.Contains(node.id);
                    if (TrainController.Shared!.CanSetSwitch(node, isThrown, out _)) {
                        node.isThrown = isThrown;
                    }
                });
            }
        }
    }
}
