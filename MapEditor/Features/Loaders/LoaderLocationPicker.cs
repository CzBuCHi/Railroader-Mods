using System.Collections;
using Helpers;
using JetBrains.Annotations;
using MapMod.Loaders;
using Track;
using UI;
using UI.Common;
using UnityEngine;

namespace MapEditor.Features.Loaders;

[PublicAPI]
public sealed class LoaderLocationPicker : MonoBehaviour
{
    private Transform  _Loader = null!;
    private Camera?    _Camera;
    private Coroutine? _Coroutine;

    private Location? _CurrentLocation;

    public void StartPickingLocation(Loader loader) {
        if (_Coroutine != null) {
            StopCoroutine(_Coroutine);
        }

        _Loader = loader.transform;
        _CurrentLocation = loader.Location;

        _Coroutine = StartCoroutine(Loop());
        ShowMessage("Click to set loader location");
        
        GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Transient, DidEscape);
    }

    private bool DidEscape() {
        ShowMessage("Cancelled");
        StopLoop();
        return true;
    }

    private void StopLoop() {
        if (_Coroutine != null) {
            StopCoroutine(_Coroutine);
            _Coroutine = null;
        }

        GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Transient);
        _Loader.gameObject.SetActive(false);
    }

    private void ShowMessage(string message) => Toast.Present(message, ToastPosition.Bottom);

    private IEnumerator Loop() {
        Location? location;
        while (true) {
            location = HitLocation();
            if (location.HasValue) {
                var positionRotation = Graph.Shared.GetPositionRotation(location.Value);
                _Loader.position = WorldTransformer.GameToWorld(positionRotation.Position);
                _Loader.rotation = positionRotation.Rotation;
                _Loader.gameObject.SetActive(true);
                if (!_CurrentLocation.Equals(location.Value) && Input.GetMouseButtonDown(0)) {
                    break;
                }
            } else {
                _Loader.gameObject.SetActive(false);
            }

            yield return null;
        }

        _CurrentLocation = location;
        StopLoop();
    }

    private Location? HitLocation() {
        if (!MainCameraHelper.TryGetIfNeeded(ref _Camera)) {
            return null;
        }

        return Graph.Shared.LocationFromMouse(_Camera);
    }
}
