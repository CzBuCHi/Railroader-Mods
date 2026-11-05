using System.IO;
using System;
using Relay.Native;
using Relay;
using Tweaks.Tests;
using Tweaks.Units;

namespace Tweaks.Tests.Units
{
    internal static class TutorialDistanceTests
    {
        internal static void Run(string managed)
        {
            var assets = Path.GetFullPath(Path.Combine(managed, "..", "StreamingAssets"));
            var path = Path.Combine(assets, "Tutorial", "pagecontext.rl");
            var original = File.ReadAllText(path);
            var modified = TutorialDistanceSource.InMeters(original);
            Program.Check(TutorialDistanceSource.IsTutorialContext(path, assets), "Only installed tutorial context is selected");
            Program.Check(!TutorialDistanceSource.IsTutorialContext(Path.Combine(assets, "Mods", "pagecontext.rl"), assets) &&
                !TutorialDistanceSource.IsTutorialContext(Path.Combine(assets, "Tutorial", "chapter.rl"), assets) &&
                !TutorialDistanceSource.IsTutorialContext(null!, assets), "Unrelated Relay modules are excluded");
            Program.Check(modified != original && !modified.Contains("math.ceil(feet)") && !modified.Contains("math.floor(feet + 0.5)"),
                "Both installed imperial helper implementations are replaced");
            var after = original.IndexOf("// Helper: Check if value", StringComparison.Ordinal);
            Program.Check(modified.Substring(modified.IndexOf("// Helper: Check if value", StringComparison.Ordinal)) == original.Substring(after),
                "All tutorial conditions, logic and prose remain unchanged");
            var interpreter = new Interpreter();
            interpreter.RegisterNamedModule(new NativeModule("math")
                .DefineFunction("floor", 1, args => Value.Float((float)Math.Floor(args[0].AsFloat())))
                .DefineFunction("ceil", 1, args => Value.Float((float)Math.Ceiling(args[0].AsFloat()))));
            var modules = new StringModuleLoader();
            modules.RegisterModule("arrowoverlay", "class ArrowOverlay {}\nexport ArrowOverlay\n");
            interpreter.RegisterModuleLoader(modules);
            interpreter.RegisterNamedModule(new NativeModule("railroader").DefineConstant("world", Value.Bool(false)).DefineConstant("ui", Value.Bool(false)));
            interpreter.Interpret(modified + "\nvar rounded = distanceString(30.48)\nvar ceiling = distanceStringCeil(30.48)\n" +
                "var longDistance = distanceStringCeil(1609.344)\nvar exact = distanceStringCeil(0)\nvar boundary = distanceString(30.5)\n" +
                "var belowKm = distanceString(149)\nvar kmThreshold = distanceString(150)\nvar roundedKm = distanceString(1601)\n" +
                "var ceilingKm = distanceStringCeil(1601)\n", "tutorial-metric-test.rl");
            Program.Check(interpreter.GetGlobal("rounded").AsString() == "30 m", "Actual Relay nearest-meter helper runs");
            Program.Check(interpreter.GetGlobal("ceiling").AsString() == "31 m", "Actual Relay remaining-distance helper rounds up");
            Program.Check(interpreter.GetGlobal("longDistance").AsString() == "1.61 km", "Actual Relay long distance uses kilometers");
            Program.Check(interpreter.GetGlobal("belowKm").AsString() == "149 m" && interpreter.GetGlobal("kmThreshold").AsString() == "0.15 km",
                "Actual Relay helpers switch to kilometers at 150 meters");
            Program.Check(interpreter.GetGlobal("roundedKm").AsString() == "1.6 km" && interpreter.GetGlobal("ceilingKm").AsString() == "1.61 km",
                "Actual Relay kilometer helper preserves nearest/ceiling distinction");
            Program.Check(interpreter.GetGlobal("exact").AsString() == "0 m" && interpreter.GetGlobal("boundary").AsString() == "31 m",
                "Relay zero and half-meter rounding follow tutorial semantics");
            foreach (var changed in new[] { "func somethingElse(meters) {}", original.Replace("func distanceStringCeil(meters)", "func renamed(meters)") })
            {
                var rejected = false;
                try { TutorialDistanceSource.InMeters(changed); } catch (NotSupportedException) { rejected = true; }
                Program.Check(rejected, "Unexpected tutorial helper layout is rejected without partial rewriting");
            }
            Program.Check(File.ReadAllText(path) == original, "Installed tutorial file remains unchanged");
        }
    }
}
