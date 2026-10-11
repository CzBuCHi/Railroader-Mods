using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TurntableControl;

internal sealed class TurntableManager : MonoBehaviour
{
    private readonly List<TurntableDriver> drivers = new List<TurntableDriver>();
    private bool mapLoaded;
    private bool registered;
    private float nextScan;

    internal void Initialize()
    {
        Messenger.Default.Register<MapDidLoadEvent>(this, OnMapLoaded);
        Messenger.Default.Register<MapWillUnloadEvent>(this, OnMapWillUnload);
        Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapUnloaded);
        registered = true;
    }

    private void OnMapLoaded(MapDidLoadEvent message)
    {
        mapLoaded = true;
        nextScan = Time.unscaledTime + 0.5f;
    }

    private void OnMapWillUnload(MapWillUnloadEvent message) => Clear();
    private void OnMapUnloaded(MapDidUnloadEvent message) => Clear();

    private void Update()
    {
        if (!mapLoaded || StateManager.IsUnloading || StateManager.Shared == null ||
            Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 1f;
        for (var i = drivers.Count - 1; i >= 0; i--)
        {
            var oldDriver = drivers[i];
            if (oldDriver != null && oldDriver.CanStayAttached)
                continue;
            if (oldDriver != null)
            {
                oldDriver.Shutdown();
                Object.Destroy(oldDriver);
            }
            drivers.RemoveAt(i);
        }
        foreach (var controller in Object.FindObjectsOfType<TurntableController>())
        {
            if (!controller.isActiveAndEnabled || controller.turntable == null ||
                controller.GetComponent<TurntableDriver>() != null)
                continue;

            TurntableDriver? driver = null;
            try
            {
                driver = controller.gameObject.AddComponent<TurntableDriver>();
                driver.Initialize(controller);
                drivers.Add(driver);
                Debug.Log("[TurntableControl] Enabled turntable " + controller.turntable.id);
            }
            catch (Exception error)
            {
                if (driver != null)
                {
                    driver.Shutdown();
                    Object.Destroy(driver);
                }
                Debug.LogError("[TurntableControl] Could not initialize turntable: " + error);
            }
        }

        foreach (var driver in drivers)
            if (driver != null)
            {
                try
                {
                    driver.RefreshMarkers();
                }
                catch (Exception error)
                {
                    Debug.LogError("[TurntableControl] Could not refresh track markers: " + error);
                }
            }
    }

    private void Clear()
    {
        mapLoaded = false;
        TurntableMarker.SetHovered(null);
        foreach (var driver in drivers)
        {
            if (driver == null)
                continue;
            driver.Shutdown();
            Object.Destroy(driver);
        }
        drivers.Clear();
    }

    internal void Shutdown()
    {
        if (registered)
        {
            Messenger.Default.Unregister(this);
            registered = false;
        }
        Clear();
    }

    private void OnDestroy() => Shutdown();
}
