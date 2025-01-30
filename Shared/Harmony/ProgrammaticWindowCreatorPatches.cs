using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Serilog;
using UI;
using UI.Common;

namespace CzBuCHi.Shared.Harmony;

[PublicAPI]
[HarmonyPatch]
public static partial class ProgrammaticWindowCreatorPatches
{
    private static MethodInfo                  _CreateWindow;
    private static Dictionary<Type, Delegate?> _RegisteredWindows = new();

    static ProgrammaticWindowCreatorPatches() {
        var methodInfo =
            typeof(ProgrammaticWindowCreator)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(o => o.IsGenericMethod && o.Name == "CreateWindow" && o.GetParameters().Length == 1);

        if (methodInfo == null) {
            throw new InvalidOperationException("Cannot find method UI.ProgrammaticWindowCreator:CreateWindow<TWindow>(Action<>)");
        }

        _CreateWindow = methodInfo;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProgrammaticWindowCreator), "Start")]
    public static void Start(ProgrammaticWindowCreator __instance) {
        foreach (var pair in _RegisteredWindows) {
            Log.Information("CreateWindow: " + pair.Key);
            _CreateWindow.MakeGenericMethod(pair.Key!).Invoke(__instance, [pair.Value]);
        }
    }

    public static void RegisterWindow<TWindow>(Action<TWindow>? configure = null) {
        Log.Information("RegisterWindow: " + typeof(TWindow));
        _RegisteredWindows[typeof(TWindow)] = configure;
    }
}
