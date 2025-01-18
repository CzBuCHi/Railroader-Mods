using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Helpers;
using JetBrains.Annotations;
using UnityEngine;

namespace MapEditor.Utility;

[UsedImplicitly]
public sealed class UnityHelpers : MonoBehaviour
{
    private static UnityHelpers? _Instance;

    public static void Initialize() {
        var go = new GameObject("UnityHelpers");
        _Instance = go.AddComponent<UnityHelpers>();
    }

    public static void Destroy() {
        if (_Instance != null) {
            Destroy(_Instance);
            _Instance = null;
        }
    }

    private static void StartStaticCoroutine(IEnumerator coroutine) {
        _Instance!.StartCoroutine(coroutine);
    }

    public static void CallOnNextFrame(Action action) {
        StartStaticCoroutine(CreateCoroutine());
        return;

        IEnumerator CreateCoroutine() {
            yield return new WaitForEndOfFrame();
            action();
        }
    }

    // Get the current mouse position projected onto the drag plane
    public static Vector3? GetMouseDragOffset(Plane dragPlane, Camera camera, Vector3 dragStartPosition) {
        var mouseRay = camera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(mouseRay, out var enter)) {
            var currentPoint = mouseRay.GetPoint(enter);
            return currentPoint - dragStartPosition;
        }
        return null;
    }

    // Raycast from the mouse to determine a point in the world
    public static bool RayPointFromMouse(Camera camera, out Vector3 point) {
        var mouseRay = camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out var hitInfo, Mathf.Infinity, 1 << Layers.Terrain)) {
            point = hitInfo.point;
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    private static readonly ConcurrentDictionary<int, List<Action>> _CallOnceOnMouseButtonHandlers = new();

    public void Update() {
        var handlers = _CallOnceOnMouseButtonHandlers;
        if (handlers.Count == 0) {
            return;
        }

        foreach (var pair in handlers) {
            if (!Input.GetMouseButton(pair.Key)) {
                continue;
            }

            var actions = pair.Value!.ToArray();
            pair.Value.Clear();
            foreach (var action in actions) {
                action();
            }
        }
        
    }

    // Calls action once when user press mouse button
    public static void CallOnceOnMouseButton(int button, Action action) {
        var list = _CallOnceOnMouseButtonHandlers.GetOrAdd(button, new List<Action>())!;
        list.Add(action);
    }
}
