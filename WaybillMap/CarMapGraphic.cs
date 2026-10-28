using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WaybillMap
{
    internal readonly struct MapMarker
    {
        internal readonly MarkerGeometry Geometry;
        internal readonly Color Color;
        internal readonly string Number;
        internal readonly string Label;
        internal MapMarker(MarkerGeometry geometry, Color color, string label, string number)
        { Geometry = geometry; Color = color; Label = label; Number = number; }
    }

    internal sealed class CarMapGraphic : MaskableGraphic
    {
        private List<MapMarker>? markers;
        private int start, count;

        internal void SetMarkers(List<MapMarker> items, int offset, int length)
        {
            raycastTarget = false;
            markers = items; start = offset; count = length;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (markers == null) return;
            var end = Math.Min(markers.Count, start + count);
            for (var i = start; i < end; i++)
            {
                var marker = markers[i];
                Quad(mesh, marker.Geometry, 0.75, new Color32(24, 24, 20, 255));
                Quad(mesh, marker.Geometry, 0, marker.Color);
            }
        }

        private static void Quad(VertexHelper mesh, MarkerGeometry g, double padding, Color color)
        {
            var along = new Vector2((float)g.AxisX, (float)g.AxisY) * (float)(g.HalfLength + padding);
            var across = new Vector2((float)-g.AxisY, (float)g.AxisX) * (float)(g.HalfWidth + padding);
            var center = new Vector2((float)g.X, (float)g.Y);
            var start = mesh.currentVertCount;
            mesh.AddVert(center - along - across, color, Vector2.zero);
            mesh.AddVert(center - along + across, color, Vector2.zero);
            mesh.AddVert(center + along + across, color, Vector2.zero);
            mesh.AddVert(center + along - across, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
