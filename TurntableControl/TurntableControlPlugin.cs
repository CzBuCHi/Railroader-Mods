using HarmonyLib;
using RailForge;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TurntableControl;

public sealed class TurntableControlPlugin : PluginBase
{
    private Harmony? harmony;
    private TurntableManager? manager;

    public override void OnEnable()
    {
        if (manager != null)
            return;

        NativeAccess.Validate();
        harmony = new Harmony("CzBuCHi.TurntableControl");
        harmony.PatchAll(typeof(TurntableControlPlugin).Assembly);
        var host = new GameObject("CzBuCHi.TurntableControl.Manager");
        Object.DontDestroyOnLoad(host);
        manager = host.AddComponent<TurntableManager>();
        manager.Initialize();
    }

    public override void OnDisable()
    {
        if (manager != null)
        {
            manager.Shutdown();
            Object.Destroy(manager.gameObject);
            manager = null;
        }
        harmony?.UnpatchAll("CzBuCHi.TurntableControl");
        harmony = null;
    }
}
