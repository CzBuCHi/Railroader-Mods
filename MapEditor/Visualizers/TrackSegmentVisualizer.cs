using System.Linq;
using System.Text;
using Core;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Behaviours;
using MapEditor.MapState.AutoTrestleEditor;
using MapEditor.MapState.AutoTrestleEditor.StrangeCustoms;
using MapEditor.Utility;
using Serilog;
using Track;
using UnityEngine;
using static InfinityCode.RealWorldTerrain.Utils.RealWorldTerrainGPXObject;
using TrackSegment = Track.TrackSegment;

namespace MapEditor.Visualizers;

[PublicAPI]
public sealed class TrackSegmentVisualizer : MonoBehaviour, IPickable
{
    #region Manage

    public static void CreateVisualizers() {
        foreach (var segment in Graph.Shared.Segments) {
            CreateTrackSegmentVisualizer(segment);
        }
    }

    public static TrackSegmentVisualizer CreateTrackSegmentVisualizer(TrackSegment trackSegment) {
        var go = new GameObject("TrackSegmentVisualizer_" + trackSegment.id);
        go.transform.SetParent(trackSegment.transform);
        go.SetActive(false);
        return go.AddComponent<TrackSegmentVisualizer>();
    }

    public static void DestroyVisualizers() {
        var visualizers = Graph.Shared.GetComponentsInChildren<TrackSegmentVisualizer>(true)!;
        foreach (var visualizer in visualizers) {
            Destroy(visualizer.gameObject);
        }
    }

    public static void ShowVisualizers(TrackNode trackNode) {
        foreach (var trackSegment in Graph.Shared.SegmentsConnectedTo(trackNode)) {
            ShowVisualizer(trackSegment);
        }
    }

    public static void ShowVisualizer(TrackSegment trackSegment) {
        var visualizer = trackSegment.GetComponentInChildren<TrackSegmentVisualizer>(true) ?? CreateTrackSegmentVisualizer(trackSegment);
        visualizer.gameObject.SetActive(true);
    }

    public static void HideVisualizers(TrackNode trackNode) {
        foreach (var trackSegment in Graph.Shared.SegmentsConnectedTo(trackNode)) {
            HideVisualizer(trackSegment);
        }
    }

    public static void HideVisualizer(TrackSegment trackSegment) {
        var visualizer = trackSegment.GetComponentInChildren<TrackSegmentVisualizer>(true);
        if (visualizer != null) {
            visualizer.gameObject.SetActive(false);
        }
    }

    #endregion

    private static readonly Color         _Yellow       = new(1, 1, 0);
    private readonly        Material      _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);
    private                 LineRenderer? _LineRenderer1;
    private                 LineRenderer? _LineRenderer2;
    private                 TrackSegment  _TrackSegment = null!;
    private                 BoxCollider   _BoxCollider  = null!;
    private                 GameObject?   _Sleeper;

    public bool PendingRebuild { get; set; }

    public void Awake() {
        _TrackSegment = gameObject.GetComponentInParent<TrackSegment>()!;

        transform.localPosition = -transform.parent.localPosition;
        transform.localEulerAngles = Vector3.zero;

        gameObject.layer = Layers.Clickable;

        var lineObject1 = new GameObject();
        lineObject1.transform.SetParent(gameObject.transform);
        _LineRenderer1 = AddLineRenderer(lineObject1);

        var lineObject2 = new GameObject();
        lineObject2.transform.SetParent(gameObject.transform);
        _LineRenderer2 = AddLineRenderer(lineObject2);

        _BoxCollider = gameObject.AddComponent<BoxCollider>();
        _BoxCollider.size = new Vector3(1, 1, 1);
    }

    public void Start() {
        RebuildBezier();
    }

    public void Update() {
        var isSelected = EditorState.TrackSegment == _TrackSegment;
        _LineMaterial.color = isSelected ? Color.green : _Yellow;

        if (PendingRebuild) {
            RebuildBezier();
            if (_TrackSegment.style == TrackSegment.Style.Bridge) {
                var data = MapEditorPlugin.PatchEditor!.GetAutoTrestle(_TrackSegment)!;
                if (data != null) {
                    MapEditorPlugin.PatchEditor!.AddOrUpdateAutoTrestle(_TrackSegment, AutoTrestleUtility.CreateOrUpdate(_TrackSegment, data.HeadStyle, data.TailStyle));
                    AutoTrestleUtility.UpdateTrestle(_TrackSegment);
                }
            }

            PendingRebuild = false;
        }
    }

    public void RebuildBezier() {
        var curve = _TrackSegment.CreateBezier();

        _BoxCollider.center = curve.GetPoint(0.5f);

        var (leftPositions, rightPositions) = GetOffsetPoints(curve);

        var zero = leftPositions[0];
        leftPositions = leftPositions.Select(o => {
            var vec = o - zero;
            return new Vector3(vec.x, vec.z, -vec.y);
        }).ToArray();

        rightPositions = rightPositions.Select(o => {
            var vec = o - zero;
            return new Vector3(vec.x, vec.z, -vec.y);
        }).ToArray();

        // Update the LineRenderer
        _LineRenderer1!.positionCount = leftPositions.Length;
        _LineRenderer1.SetPositions(leftPositions);
        _LineRenderer1.transform.SetLocalPositionAndRotation(zero, Quaternion.Euler(90, 0, 0));

        _LineRenderer2!.positionCount = rightPositions.Length;
        _LineRenderer2.SetPositions(rightPositions);
        _LineRenderer2.transform.SetLocalPositionAndRotation(zero, Quaternion.Euler(90, 0, 0));

        if (_Sleeper != null) {
            Destroy(_Sleeper);
        }

        _Sleeper = CreateSleeper(curve, 0.5f);
    }

    private (Vector3[] leftPoints, Vector3[] rightPoints) GetOffsetPoints(BezierCurve curve) {
        const int   pointCount     = 20;
        const float px             = 1f / (pointCount - 1);
        const float offsetDistance = 0.65f;

        var leftPoints  = new Vector3[pointCount];
        var rightPoints = new Vector3[pointCount];
        var p           = 0f;

        for (var i = 0; i < pointCount; i++, p += px) {
            var point     = curve.GetPoint(p);
            var tangent   = curve.GetDirection(p).normalized;
            var offsetDir = Vector3.Cross(Vector3.up, tangent).normalized;

            leftPoints[i] = point - offsetDir * offsetDistance;
            rightPoints[i] = point + offsetDir * offsetDistance;
        }

        // Ensure that the last point (p = 1) is included
        var finalPoint     = curve.GetPoint(1f);
        var finalTangent   = curve.GetDirection(1f).normalized;
        var finalOffsetDir = Vector3.Cross(Vector3.up, finalTangent).normalized;

        leftPoints[pointCount - 1] = finalPoint - finalOffsetDir * offsetDistance;
        rightPoints[pointCount - 1] = finalPoint + finalOffsetDir * offsetDistance;

        return (leftPoints, rightPoints);
    }

    private LineRenderer AddLineRenderer(GameObject targetObject) {
        var lineRenderer = targetObject.AddComponent<LineRenderer>();
        lineRenderer.material = _LineMaterial;
        lineRenderer.startWidth = 0.075f;
        lineRenderer.endWidth = 0.075f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        return lineRenderer;
    }

    private GameObject CreateSleeper(BezierCurve curve, float p) {
        const float x = 1.0f;
        const float y = 0.01f;
        const float z = 0.1f;

        var sleeper = new GameObject("Sleeper");
        sleeper.transform.SetParent(gameObject.transform);
        sleeper.transform.localPosition = curve.GetPoint(p) + new Vector3(0, y, 0);
        sleeper.transform.localRotation = curve.GetRotation(p);

        var mesh = new Mesh {
            vertices = [new Vector3(x, 0.011f, z), new Vector3(-x, 0.011f, z), new Vector3(-x, 0.011f, -z), new Vector3(x, 0.011f, -z)],
            triangles = [2, 1, 0, 0, 3, 2]
        };

        var meshFilter = sleeper.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshRenderer = sleeper.AddComponent<MeshRenderer>();
        meshRenderer.material = _LineMaterial;

        return sleeper;
    }

    private TooltipInfo BuildTooltipInfo() {
        var sb = new StringBuilder();
        sb.AppendLine($"ID: {_TrackSegment.id}");
        sb.AppendLine($"Start: {_TrackSegment.a.id}");
        sb.AppendLine($"End: {_TrackSegment.b.id}");
        sb.AppendLine($"Length: {_TrackSegment.Curve.CalculateLength()}m");
        return new TooltipInfo($"Segment {_TrackSegment.id}", sb.ToString());
    }

    #region IPickable

    public void Activate(PickableActivateEvent evt) {
        Log.Information("SelectedAsset = " + _TrackSegment);
        EditorState.Update(state => state with { SelectedAsset = _TrackSegment });
    }

    public void Deactivate() {
    }

    public float                    MaxPickDistance  => EditorState.SelectedPatch != null ? 200f : 0f;
    public int                      Priority         => 1;
    public TooltipInfo              TooltipInfo      => EditorState.SelectedPatch != null ? BuildTooltipInfo() : TooltipInfo.Empty;
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    #endregion
}
