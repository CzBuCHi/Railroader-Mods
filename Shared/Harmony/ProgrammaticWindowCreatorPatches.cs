using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UI;

namespace CzBuCHi.Shared.Harmony;

[PublicAPI]
[HarmonyPatch]
public static class ProgrammaticWindowCreatorPatches
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
            _CreateWindow.MakeGenericMethod(pair.Key!).Invoke(__instance, [pair.Value]);
        }
    }

    public static void RegisterWindow<TWindow>(Action<TWindow>? configure = null) {
        _RegisteredWindows[typeof(TWindow)] = configure;
    }
}
