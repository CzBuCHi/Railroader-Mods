using System.Collections.Generic;
using System.Linq;
using System;
using Game.Messages;
using Game.State;
using Railroader.Modding;
using Track;
using Tweaks.MapTweaks.Configuration;

namespace Tweaks.MapTweaks.Switches
{
    internal sealed class SwitchActions
    {
        private readonly MapSettingsStore profiles = new MapSettingsStore(global::Tweaks.Configuration.Settings.FilePath);
        private readonly IModLogger log;
        internal string Status { get; private set; } = "Switch positions";
        internal SwitchActions(IModLogger logger) { log = logger; }

        internal void Save() => Run(() =>
        {
            var graph = Graph.Shared ?? throw new InvalidOperationException("Load a world first.");
            var ids = graph.Nodes.Where(n => n != null && graph.IsSwitch(n) && n.isThrown).Select(n => n.id).ToArray();
            profiles.SaveSwitches(ids);
            return "Saved " + ids.Length + " reversed switches";
        });

        internal void Restore() => Run(() =>
        {
            var graph = Graph.Shared ?? throw new InvalidOperationException("Load a world first.");
            var controller = TrainController.Shared;
            if (controller == null || StateManager.Shared == null) throw new InvalidOperationException("World is not ready.");
            var ids = new HashSet<string>(profiles.Load().ThrownSwitches, StringComparer.Ordinal);
            var changed = 0; var skipped = 0;
            foreach (var node in graph.Nodes.Where(n => n != null && graph.IsSwitch(n)))
            {
                var target = ids.Contains(node.id);
                if (node.isThrown == target) continue;
                var message = new RequestSetSwitch(node.id, target);
                if ((node.IsCTCSwitch && !node.IsCTCSwitchUnlocked) || !controller.CanSetSwitch(node, target, out _) ||
                    !StateManager.CheckAuthorizedToSendMessage(message)) { skipped++; continue; }
                StateManager.ApplyLocal(message); changed++;
            }
            return "Requested " + changed + "; skipped " + skipped;
        });

        private void Run(Func<string> action)
        {
            try { Status = action(); log.Info(Status); }
            catch (Exception error) { Status = "Switch action failed (see log)"; log.Error(error, "Switch action failed; saved profile was not overwritten."); }
        }
    }
}
