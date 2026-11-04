using System;
using System.Collections;
using Game.State;
using Map.Runtime;
using UI.Common;
using UI.Windows;
using UnityEngine;

namespace Tweaks.PointsOfInterest
{
    internal sealed class PointsOfInterestRuntime : MonoBehaviour
    {
        private PointsOfInterestFeature plugin = null!;
        private bool mapLoaded;
        private Coroutine? jump;
        private ProgressScope? progress;

        internal void Initialize(PointsOfInterestFeature owner)
        {
            plugin = owner;
        }

        internal void SetMapLoaded(bool loaded)
        {
            mapLoaded = loaded;
            if (!loaded)
            {
                StopAllCoroutines(); jump = null;
                progress?.Dispose(); progress = null;
                CloseWindow(PointsOfInterestFeature.MainWindow);
                CloseWindow(PointsOfInterestFeature.EditorWindow);
            }
        }

        private static void CloseWindow(string id)
        {
            if (WindowManager.Shared != null && CustomWindowHandle.Exists(id)) CustomWindowHandle.Create(id).Close();
        }

        internal void Toggle()
        {
            if (mapLoaded && WindowManager.Shared != null && CustomWindowHandle.Exists(PointsOfInterestFeature.MainWindow))
                CustomWindowHandle.Create(PointsOfInterestFeature.MainWindow).Toggle();
        }

        internal void Teleport(TeleportLocation location)
        {
            if (!mapLoaded || StateManager.IsUnloading) return;
            if (jump != null) StopCoroutine(jump);
            progress?.Dispose(); progress = null;
            jump = StartCoroutine(Jump(location));
        }

        private IEnumerator Jump(TeleportLocation location)
        {
            progress = ProgressScope.BeginDelayed("Loading...", 0.18f, "Points of interest");
            try
            {
                yield return MapManager.Instance.RequestPriorityLoad(CameraBookmarks.GamePosition(location));
                if (!mapLoaded || StateManager.IsUnloading) yield break;
                try { CameraBookmarks.Restore(location); }
                catch (Exception error)
                {
                    Toast.Present("Could not restore the saved camera view.");
                    plugin.Context.Log.Error(error, "Camera restore failed.");
                }
            }
            finally { progress?.Dispose(); progress = null; jump = null; }
        }

        private void OnDestroy() => SetMapLoaded(false);
    }
}
