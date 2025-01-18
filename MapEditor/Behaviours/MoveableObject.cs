using System;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Harmony;
using MapEditor.Utility;
using UI;
using UnityEngine;

namespace MapEditor.Behaviours;

[PublicAPI]
internal sealed class MoveableObject : MonoBehaviour
{
    #region Manage

    private static MoveableObject? _Instance;

    public static MoveableObject? Shared => _Instance;

    public static void Create(GameObject target, MoveableObjectMode mode,  Action<Vector3, Quaternion>? onComplete) => Create(target, mode, null, onComplete);

    public static void Create(GameObject target, MoveableObjectMode mode, Action<Vector3, Quaternion>? onUpdate, Action<Vector3, Quaternion>? onComplete) {
        Destroy();
        _Instance = target.AddComponent<MoveableObject>();
        _Instance.OnUpdate = (_, a, b) => onUpdate?.Invoke(a, b);
        _Instance.OnComplete = (_, a, b) => onComplete?.Invoke(a, b);
        _Instance.Mode = mode;
    }

    public static MoveableObject CreateX(GameObject target) {
        Destroy();

        _Instance = target.AddComponent<MoveableObject>();
        return _Instance;
    }

    public static void Destroy() {
        if (_Instance != null) {
            Destroy(_Instance);
            _Instance = null;
        }
    }

    #endregion



    public MoveableObjectMode Mode {
        get => _Mode;
        set {
            if (_Mode == value) {
                return;
            }

            _Mode = value;
            enabled = _Mode != MoveableObjectMode.None;
        }
    }

    public Action<MoveableObjectMode, Vector3, Quaternion>? OnComplete;
    public Action<MoveableObjectMode, Vector3, Quaternion>? OnUpdate;

    private MoveableObjectMode _Mode;
    private Vector3?           _DragStartPosition;
    private Plane              _DragPlane;
    private Camera             _MainCamera = null!;
    private Vector3            _StartPosition;
    private Quaternion         _StartRotation;
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
        if (!MainCameraHelper.TryGetIfNeeded(ref _MainCamera)) {
            return;
        }

        var mouseButtonDown = Input.GetMouseButtonDown(0) && !GameInput.IsMouseOverUI(out _, out _);
        var mouseButtonHeld = Input.GetMouseButton(0);
        var mouseButtonUp   = Input.GetMouseButtonUp(0);

        if (mouseButtonDown && UnityHelpers.RayPointFromMouse(_MainCamera, out var startPoint)) {
            _StartPosition = transform.localPosition;
            _StartRotation = transform.localRotation;

            _DragStartPosition = startPoint;
            _CumulativeVertOffset = 0;
            _DragPlane = new Plane(Vector3.up, startPoint);
            _Mode = Mode;
        } else if (mouseButtonHeld && _DragStartPosition.HasValue) {
            if (_Mode == MoveableObjectMode.Move) {
                HandleTranslation();
            } else if (_Mode == MoveableObjectMode.Rotate) {
                HandleRotation();
            }

            OnUpdate?.Invoke(_Mode, _StartPosition, _StartRotation);
        } else if (mouseButtonUp && _DragStartPosition.HasValue) {
            _DragStartPosition = null;
            OnComplete?.Invoke(_Mode, _StartPosition, _StartRotation);
        }
    }

    private void HandleTranslation() {
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 1 : 0.1f;
        _CumulativeVertOffset += Input.mouseScrollDelta.y * shift;

        Vector3? offset     = _CumulativeVertOffset != 0 ? _DragPlane.normal * _CumulativeVertOffset : null;
        var      dragOffset = UnityHelpers.GetMouseDragOffset(_DragPlane, _MainCamera, _DragStartPosition!.Value);
        if (dragOffset.HasValue) {
            offset = offset.HasValue ? offset.Value + dragOffset.Value : dragOffset;
        }

        if (offset.HasValue) {
            transform.localPosition = _StartPosition + offset.Value;
        }
    }

    private void HandleRotation() {
        var dragOffset = UnityHelpers.GetMouseDragOffset(_DragPlane, _MainCamera, _DragStartPosition!.Value);
        if (!dragOffset.HasValue) {
            return;
        }

        var currentMousePosition = _DragStartPosition.Value + dragOffset.Value;
        var startToDrag          = Vector3.ProjectOnPlane(_DragStartPosition.Value - transform.position, _DragPlane.normal);
        var currentToDrag        = Vector3.ProjectOnPlane(currentMousePosition - transform.position, _DragPlane.normal);
        var angle                = Vector3.SignedAngle(startToDrag, currentToDrag, _DragPlane.normal);
        var rotation             = Quaternion.AngleAxis(angle, _DragPlane.normal);
        transform.localRotation = _StartRotation * rotation;
    }
}

internal enum MoveableObjectMode
{
    None,
    Move,
    Rotate
}
