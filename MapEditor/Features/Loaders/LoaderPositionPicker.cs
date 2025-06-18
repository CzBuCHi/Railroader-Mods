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
public sealed class LoaderPositionPicker : MonoBehaviour
{
    private Loader     _Loader = null!;
    private Location?  _Location;
    private Camera?    _Camera;
    private Coroutine? _Coroutine;

    public void StartPickingLocation(Loader loader) {
        _Loader = loader;
        _Location = loader.Location;
        if (_Coroutine != null) {
            StopCoroutine(_Coroutine);
        }

        _Coroutine = StartCoroutine(Loop());
        ShowMessage("Click to set loader position");
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
        if (_Location != null) {
            _Loader.Location = _Location.Value;
            _Loader.UpdateTransform();
        }
    }

    private void ShowMessage(string message) => Toast.Present(message, ToastPosition.Bottom);

    private IEnumerator Loop() {
        while (true) {
            var location = HitLocation();
            if (location.HasValue) {
                _Loader.Location = location.Value;
                _Loader.UpdateTransform();
                if (Input.GetMouseButtonDown(0)) {
                    _Location = null;
                    break;
                }
            }

            yield return null;
        }

        StopLoop();
    }

    private Location? HitLocation() => !MainCameraHelper.TryGetIfNeeded(ref _Camera) ? null : Graph.Shared.LocationFromMouse(_Camera);
}
