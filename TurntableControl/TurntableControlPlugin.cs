using JetBrains.Annotations;
using Railloader;

namespace TurntableControl;

[PublicAPI]
public sealed class TurntableControlPlugin : SingletonPluginBase<TurntableControlPlugin>
{
    private const string PluginIdentifier = "CzBuCHi.TurntableControl";

    public TurntableControlPlugin(IModdingContext context, IUIHelper uiHelper) {
    }

    public override void OnEnable() {
        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.PatchAll();
    }

    public override void OnDisable() {
        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.UnpatchAll();
    }
}
