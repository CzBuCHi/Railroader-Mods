using System.Collections.Generic;
using HarmonyLib;
using Model;
using UI.Map;
using UnityEngine.UI;

namespace Tweaks.MapTweaks.Rendering
{
    // Only visibility is leased. Native positions, scales, labels and click targets remain untouched.
    internal sealed class NativeMapIcons
    {
        private readonly Dictionary<Graphic, bool> original = new Dictionary<Graphic, bool>();
        internal void Hide(IEnumerable<BaseLocomotive> engines)
        {
            foreach (var engine in engines)
            {
                if (engine == null) continue;
                var icon = (MapIcon?)AccessTools.Field(typeof(Car), "MapIcon").GetValue(engine);
                if (icon == null) continue;
                foreach (var graphic in icon.GetComponentsInChildren<Graphic>(true))
                {
                    if (!original.ContainsKey(graphic)) original.Add(graphic, graphic.enabled);
                    graphic.enabled = false;
                }
            }
        }
        internal void Restore()
        {
            foreach (var pair in original) if (pair.Key != null) pair.Key.enabled = pair.Value;
            original.Clear();
        }
    }
}
