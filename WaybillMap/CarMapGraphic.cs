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
        internal readonly bool IsLocomotive;
        internal MapMarker(MarkerGeometry geometry, Color color, string label, string number, bool engine = false)
        { Geometry = geometry; Color = color; Label = label; Number = number; IsLocomotive = engine; }
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
                Quad(mesh, marker.Geometry, 0.75, new Color32(24, 24, 20, 255), marker.IsLocomotive);
                Quad(mesh, marker.Geometry, 0, marker.Color, marker.IsLocomotive);
            }
        }

        private static void Quad(VertexHelper mesh, MarkerGeometry g, double padding, Color color, bool engine)
        {
            var along = new Vector2((float)g.AxisX, (float)g.AxisY) * (float)(g.HalfLength + padding);
            var across = new Vector2((float)-g.AxisY, (float)g.AxisX) * (float)(g.HalfWidth + padding);
            var center = new Vector2((float)g.X, (float)g.Y);
            var start = mesh.currentVertCount;
            mesh.AddVert(center - along - across, color, Vector2.zero);
            mesh.AddVert(center - along + across, color, Vector2.zero);
            mesh.AddVert(center + along + across, color, Vector2.zero);
            mesh.AddVert(center + along - across, color, Vector2.zero);
            if (engine)
            {
                var tip = center + along;
                var shoulder = along.normalized * Math.Min((float)g.HalfWidth * 0.7f, (float)g.HalfLength * 0.5f);
                // Replace front corners with shoulders and add a pointed nose inside the same bounds.
                var vertex = new UIVertex { color = color, uv0 = Vector2.zero };
                vertex.position = center + along + across - shoulder; mesh.SetUIVertex(vertex, start + 2);
                vertex.position = center + along - across - shoulder; mesh.SetUIVertex(vertex, start + 3);
                mesh.AddVert(tip, color, Vector2.zero);
                mesh.AddTriangle(start + 2, start + 4, start + 3);
            }
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
