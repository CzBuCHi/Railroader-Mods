using Helpers;
using JetBrains.Annotations;
using MapEditor.Extensions;
using MapEditor.Harmony;
using MapEditor.MapState;
using MapEditor.Utility;
using Newtonsoft.Json;
using Serilog;
using UI;
using UnityEngine;

namespace MapEditor.Behaviours;

internal interface IMoveableObjectHandler
{
    GameObject         GameObject { get; }
    MoveableObjectMode Mode       { get; }

    Vector3    StartPosition { get; }
    Quaternion StartRotation { get; }

    void OnStart();
    void OnUpdate(Vector3? translation, Quaternion? rotation);
    IStateStep OnComplete(Vector3? translation, Quaternion? rotation);
}

[PublicAPI]
internal sealed class MoveableObject : MonoBehaviour
{
    #region Manage

    public static MoveableObject? Shared { get; private set; }

    public static void Create(IMoveableObjectHandler handler) {
        Destroy();

        Shared = handler.GameObject.AddComponent<MoveableObject>();
        Shared.Handler = handler;
    }

    public static void Destroy() {
        if (Shared != null) {
            Destroy(Shared);
            Shared = null;
        }
    }

    public static MoveableObjectMode ActiveMode => Shared?.Handler?.Mode ?? MoveableObjectMode.None;

    #endregion

    private IMoveableObjectHandler? _Handler;

    public IMoveableObjectHandler? Handler {
        get => _Handler;
        set {
            if (_Handler == value) {
                return;
            }

            _Handler = value;
            enabled = _Handler != null && _Handler.Mode != MoveableObjectMode.None;
        }
    }

    private MoveableObjectMode _Mode;
    private Vector3?           _DragStartPosition;
    private Plane              _DragPlane;
    private Camera             _MainCamera = null!;
    private float              _CumulativeVertOffset;

    public void OnEnable() {
        StrategyCameraControllerPatches.DisableCameraMouseMove = true;
        StrategyCameraControllerPatches.DisableCameraMouseZoom = true;
    }

    public void OnDisable() {
        StrategyCameraControllerPatches.DisableCameraMouseMove = false;
        StrategyCameraControllerPatches.DisableCameraMouseZoom = false;
    }

    public void Update() {
        if (_Handler == null || _Handler.Mode == MoveableObjectMode.None || !MainCameraHelper.TryGetIfNeeded(ref _MainCamera)) {
            return;
        }

        var isMouseOverUi = GameInput.IsMouseOverUI(out _, out _);
        var mouseButtonDown = Input.GetMouseButtonDown(0);

        if (isMouseOverUi && mouseButtonDown) {
            Destroy();
            return;
        }

        var mouseButtonHeld = Input.GetMouseButton(0);
        var mouseButtonUp   = Input.GetMouseButtonUp(0);

        if (mouseButtonDown && UnityHelpers.RayPointFromMouse(_MainCamera, out var startPoint)) {
            _DragStartPosition = startPoint;
            _CumulativeVertOffset = 0;
            _DragPlane = new Plane(transform.up, startPoint);
            _Mode = _Handler.Mode;
            _Handler.OnStart();
        } else if (mouseButtonHeld && _DragStartPosition.HasValue) {
            var (translation, rotation) = HandleUpdate();
            if (translation != null || rotation != null) {
                _Handler.OnUpdate(translation, rotation);
            }
        } else if (mouseButtonUp && _DragStartPosition.HasValue) {
            var (translation, rotation) = HandleUpdate();
            if (translation != null || rotation != null) {
                MapStateEditor.NextStep(_Handler.OnComplete(translation, rotation));
            }
        }
    }

    private (Vector3?, Quaternion?) HandleUpdate() {
        Vector3?    translation = null;
        Quaternion? rotation    = null;
        switch (_Mode) {
            case MoveableObjectMode.Move: {
                translation = HandleTranslation();
                break;
            }
            case MoveableObjectMode.Rotate: {
                rotation = HandleRotation();
                break;
            }
        }

        return (translation, rotation);
    }

    private Vector3? HandleTranslation() {
        var shift = InputHelper.GetShift() ? 1 : 0.1f;
        _CumulativeVertOffset += Input.mouseScrollDelta.y * shift;

        Vector3? offset     = _CumulativeVertOffset != 0 ? _DragPlane.normal * _CumulativeVertOffset : null;
        var      dragOffset = UnityHelpers.GetMouseDragOffset(_DragPlane, _MainCamera, _DragStartPosition!.Value);
        if (dragOffset.HasValue) {
            dragOffset = transform.InverseTransformDirection(dragOffset.Value);

            if (InputHelper.GetControl()) {
                dragOffset = new Vector3(0, 0, dragOffset.Value.z);
            } else if (InputHelper.GetAlt()) {
                dragOffset = new Vector3(dragOffset.Value.x, 0, 0);
            }

            dragOffset = transform.TransformDirection(dragOffset.Value);

            offset = offset.HasValue ? offset.Value + dragOffset.Value : dragOffset;
        }

        return offset;
    }

    private Quaternion? HandleRotation()
    {
        var dragOffset = UnityHelpers.GetMouseDragOffset(_DragPlane, _MainCamera, _DragStartPosition!.Value);
        if (!dragOffset.HasValue)
        {
            return null;
        }

        Vector3 axis;
        if (InputHelper.GetControl()) {
            // X-axis (pitch)
            axis = Vector3.right;
        } else if (InputHelper.GetAlt()) {
            // Z-axis (roll)
            axis = Vector3.forward;
        } else {
            // Y-axis (yaw)
            axis = Vector3.up;
        }

        var startToDrag   = Vector3.ProjectOnPlane(_DragStartPosition.Value - transform.position, axis);
        var currentToDrag = Vector3.ProjectOnPlane(_DragStartPosition.Value + dragOffset.Value - transform.position, axis);
        var angle         = Vector3.SignedAngle(startToDrag, currentToDrag, axis);

        return Quaternion.AngleAxis(angle, axis);
    }
}

public enum MoveableObjectMode
{
    None,
    Move,
    Rotate
}
