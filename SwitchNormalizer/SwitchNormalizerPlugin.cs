using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Messages;
using Game.State;
using Railroader.Modding;
using Track;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwitchNormalizer
{
    public sealed class SwitchNormalizerPlugin : IModEntryPoint, IWorldLifecycle
    {
        private IModContext? context;
        private ProfileStore? profiles;
        private ToolbarButton? toolbar;

        public void OnModLoad(IModContext context)
        {
            if (this.context != null)
                return;
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            profiles = new ProfileStore(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Railroader", "ModSettings", "czbuchi", "switch-normalizer.json"));
            var host = new GameObject("CzBuCHi.SwitchNormalizer.Toolbar");
            Object.DontDestroyOnLoad(host);
            toolbar = host.AddComponent<ToolbarButton>();
            toolbar.Initialize(OnButtonClick);
            context.Log.Info("Loaded Switch Normalizer 3.0.0. Click: restore; Shift-click: save.");
        }

        public void OnWorldLoad() => toolbar?.SetMapLoaded(true);

        public void OnWorldUnload() => toolbar?.SetMapLoaded(false);

        public void OnModUnload()
        {
            if (toolbar != null)
            {
                toolbar.SetMapLoaded(false);
                Object.Destroy(toolbar.gameObject);
                toolbar = null;
            }
            profiles = null;
            context = null;
        }

        private void OnButtonClick()
        {
            if (context == null || profiles == null)
                return;
            try
            {
                var graph = Graph.Shared;
                var controller = TrainController.Shared;
                if (graph == null || controller == null || StateManager.Shared == null)
                    return;
                var switches = graph.Nodes.Where(node => node != null && graph.IsSwitch(node)).ToArray();
                if (GameInput.IsShiftDown)
                {
                    profiles.Save(new Settings
                    {
                        ThrownSwitches = switches.Where(node => node.isThrown).Select(node => node.id).ToArray()
                    });
                    context.Log.Info("Saved switch positions (" + switches.Count(node => node.isThrown) + " reversed).");
                    return;
                }

                // Reload on every restore. A corrupt/unreadable profile must abort, not normalize everything.
                var saved = profiles.Load();
                var thrown = new HashSet<string>(saved.ThrownSwitches, StringComparer.Ordinal);
                var changed = 0;
                var skipped = 0;
                foreach (var node in switches)
                {
                    var target = thrown.Contains(node.id);
                    if (node.isThrown == target)
                        continue;
                    if ((node.IsCTCSwitch && !node.IsCTCSwitchUnlocked) || !controller.CanSetSwitch(node, target, out _))
                    {
                        skipped++;
                        continue;
                    }
                    // Native requests retain multiplayer access checks and host authority.
                    StateManager.ApplyLocal(new RequestSetSwitch(node.id, target));
                    changed++;
                }
                context.Log.Info("Requested " + changed + " switch changes; skipped " + skipped + " blocked switches.");
            }
            catch (Exception error)
            {
                context.Log.Error(error, "Switch action failed; profile was not overwritten.");
            }
        }
    }
}
