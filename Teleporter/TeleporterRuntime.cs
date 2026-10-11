using System;
using System.Collections;
using Game.State;
using Map.Runtime;
using UI;
using UI.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Teleporter;

internal sealed class TeleporterRuntime : MonoBehaviour
{
    private TeleporterPlugin plugin = null!;
    private ToolbarButton toolbar = null!;
    private TeleporterWindow? view;
    private bool mapLoaded;
    private float nextAttempt;
    private Coroutine? jump;
    private bool loadingShown;

    internal void Initialize(TeleporterPlugin owner)
    {
        plugin = owner;
        toolbar = gameObject.AddComponent<ToolbarButton>();
        toolbar.Initialize(Toggle);
    }

    internal void SetMapLoaded(bool loaded)
    {
        mapLoaded = loaded;
        toolbar.SetMapLoaded(loaded);
        nextAttempt = 0f;
        if (!loaded)
        {
            HideLoading();
            StopAllCoroutines();
            jump = null;
            if (view != null)
                Object.Destroy(view.gameObject);
            view = null;
        }
    }

    private void Update()
    {
        if (!mapLoaded || view != null || Time.unscaledTime < nextAttempt)
            return;
        nextAttempt = Time.unscaledTime + 1f;
        try
        {
            var creator = Object.FindObjectOfType<ProgrammaticWindowCreator>();
            if (creator == null || creator.windowPrefab == null)
                return;
            var window = Object.Instantiate(creator.windowPrefab, creator.transform, false);
            window.name = TeleporterPlugin.Identifier + ".Window";
            view = window.gameObject.AddComponent<TeleporterWindow>();
            view.Initialize(plugin, this, window, creator.builderAssets);
            window.SetInitialPositionSize(TeleporterPlugin.Identifier + ".Window", new Vector2(560, 350),
                Window.Position.LowerRight, Window.Sizing.Resizable(new Vector2Int(560, 320)));
            window.CloseWindow();
            if (plugin.Settings.AutoOpenTeleporterWindow)
                view.Show();
        }
        catch (Exception error)
        {
            if (view != null)
                Object.Destroy(view.gameObject);
            view = null;
            nextAttempt = Time.unscaledTime + 5f;
            Debug.LogError("[Teleporter] Could not create window: " + error);
        }
    }

    private void Toggle()
    {
        if (view != null)
            view.Toggle();
    }

    internal void Teleport(TeleportLocation location)
    {
        if (!mapLoaded || StateManager.IsUnloading)
            return;
        if (jump != null)
        {
            StopCoroutine(jump);
            HideLoading();
        }
        jump = StartCoroutine(Jump(location));
    }

    private IEnumerator Jump(TeleportLocation location)
    {
        // Match the native map teleport: delay the overlay so nearby jumps stay quiet.
        ProgressIndicator.Show("Loading...", 0.18f);
        loadingShown = true;
        var started = Time.unscaledTime;
        try
        {
            yield return MapManager.Instance.RequestPriorityLoad(CameraBookmarks.GamePosition(location));
            if (!mapLoaded || StateManager.IsUnloading)
                yield break;
            var needsProgress = Time.unscaledTime - started > 0.15f;
            if (!needsProgress)
                HideLoading();
            try { CameraBookmarks.Restore(location); }
            catch (Exception error)
            {
                Toast.Present("Could not restore the saved camera view.");
                Debug.LogError("[Teleporter] Camera restore failed: " + error);
            }
            // Native teleport leaves the overlay briefly visible after a slow load.
            if (needsProgress)
                yield return new WaitForSecondsRealtime(0.5f);
        }
        finally
        {
            HideLoading();
            jump = null;
        }
    }

    private void HideLoading()
    {
        if (!loadingShown)
            return;
        loadingShown = false;
        // The UI may already have been destroyed during map unload.
        if (Object.FindObjectOfType<ProgressIndicator>() != null)
            ProgressIndicator.Hide();
    }

    private void OnDestroy() => SetMapLoaded(false);
}