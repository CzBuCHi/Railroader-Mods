using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CarInspectorTweaks.Utility;

public static class TranspilerUtils {
    public static void LogAllInstructions(MethodBase original, IEnumerable<CodeInstruction> instructions) {
        Log.Information("----------------\r\n" + original.DeclaringType.FullName + "::" + original.Name + "\r\n\r\n"
                        + string.Join("\r\n", instructions.Select(ToString)) +
                        "\r\n\r\n----------------"
        );
    }

    public static void LogAllMembers(Type type) {
        var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

        Log.Information("----------------\r\n" + type.FullName + "\r\n\r\n"
                        + string.Join("\r\n", members.Select(Selector)) +
                        "\r\n\r\n----------------"
        );
        return;

        string Selector(MemberInfo o) => o.MemberType.ToString().PadRight(10) + " " + o;
    }

    private static string ToString(CodeInstruction instructions) {
        var str   = instructions.ToString();
        var index = str.IndexOf(' ');
        if (index == -1) {
            return str.ToUpperInvariant();
        }

        return str.Substring(0, index).ToUpperInvariant().PadRight(10) + str.Substring(index);
    }
}
