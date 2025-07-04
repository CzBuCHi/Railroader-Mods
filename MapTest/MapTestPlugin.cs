using HarmonyLib;
using JetBrains.Annotations;
using Railloader;

namespace MapTest;

[PublicAPI]
public sealed class MapTestPlugin(IModdingContext context, IUIHelper uiHelper) : SingletonPluginBase<MapTestPlugin>
{
    private const string ModIdentifier = "CzBuCHi.MapTest";

    public override void OnEnable() {
        var harmony     = new Harmony(ModIdentifier);
        harmony.PatchAll();
    }

    public override void OnDisable() {
        var harmony = new Harmony(ModIdentifier);
        harmony.UnpatchAll();
    }
}
