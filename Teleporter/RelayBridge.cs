using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Scripting;
using Game.Scripting.Modding;
using HarmonyLib;
using Relay;
using Relay.Native;
using TMPro;
using UI.Builder;
using UI.Builder.Relay;
using UnityEngine;
using Value = Relay.Value;

namespace Teleporter
{
    [HarmonyPatch(typeof(ScriptInterpreterFactory), nameof(ScriptInterpreterFactory.CreatePackScopedBuilder))]
    internal static class RelayBridge
    {
        private static readonly FieldInfo BuilderField = typeof(RelayUIPanelBuilder).GetField("_builder",
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NotSupportedException("Relay builder field was changed.");

        internal static void Validate()
        {
            if (BuilderField.FieldType != typeof(UIPanelBuilder))
                throw new NotSupportedException("Unsupported Relay UI builder.");
        }

        [HarmonyPostfix]
        private static void Postfix(string packIdentifier, ScriptCapabilityTier tier, ref IInterpreterBuilderFluent __result)
        {
            // Expose the native bridge only to this pack's full runtime scripts.
            if (packIdentifier == TeleporterPlugin.Identifier && tier == ScriptCapabilityTier.Full && TeleporterPlugin.Active != null)
                __result = __result.RegisterNamedModule(CreateModule());
        }

        internal static NativeModule CreateModule()
        {
            var module = new NativeModule("teleporter");
            module.DefineFunction("groups", 0, _ => Strings(Plugin.Store.Current.Groups.Select(group => group.Name)));
            module.DefineFunction("locations", 1, args => Strings(Plugin.Store.Current.Groups
                .FirstOrDefault(group => group.Name == args[0].AsString())?.Locations.Keys
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase) ?? Enumerable.Empty<string>()));
            module.DefineFunction("add", 2, args => Action(() => Plugin.Store.Add(args[0].AsString(), args[1].AsString(), CameraBookmarks.Capture())));
            module.DefineFunction("replace", 2, args => Action(() => Plugin.Store.Replace(args[0].AsString(), args[1].AsString(), CameraBookmarks.Capture())));
            module.DefineFunction("remove", 2, args => Action(() => Plugin.Store.Delete(args[0].AsString(), args[1].AsString())));
            module.DefineFunction("moveGroup", 2, args => Action(() => Plugin.Store.MoveGroup(args[0].AsString(), args[1].AsInt())));
            module.DefineFunction("autoOpen", 0, _ => Value.Bool(Plugin.Store.Current.AutoOpenTeleporterWindow));
            module.DefineFunction("setAutoOpen", 1, args => Action(() => Plugin.Store.SetAutoOpen(args[0].AsBool())));
            module.DefineFunction("locationRow", 4, args =>
            {
                var builder = Builder(args[0]);
                var group = args[2].AsString(); var name = args[3].AsString();
                builder.AddLocationFieldFallback(args[1].AsString(), () =>
                {
                    try { Plugin.Runtime.Teleport(Plugin.Store.Get(group, name)); }
                    catch (Exception error) { Report(error); }
                });
                return Value.Null();
            });
            // Temporary input adapter. Replace this one script call when Relay gains native text inputs.
            module.DefineFunction("inputField", 4, args =>
            {
                var builder = Builder(args[0]);
                var callContext = Interpreter.CurrentCallContext;
                var closure = args[3].AsClosure();
                var input = builder.AddInputField(args[2].AsString(), _ => { }, "", 80);
                input.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
                {
                    try { callContext.CallClosure(closure, new[] { Value.String(value) }); }
                    catch (Exception error) { Report(error); }
                });
                builder.AddField(args[1].AsString(), input);
                return Value.Null();
            });
            return module;
        }

        private static TeleporterPlugin Plugin => TeleporterPlugin.RequireActive();
        private static Value Strings(IEnumerable<string> items) => Value.Array(items.Select(Value.String).ToList());
        private static UIPanelBuilder Builder(Value value)
        {
            var wrapper = value.AsTypeInstance().NativeObject as RelayUIPanelBuilder
                ?? throw new ArgumentException("Expected a Relay UIPanelBuilder.");
            return (UIPanelBuilder)BuilderField.GetValue(wrapper);
        }
        private static Value Action(System.Action action)
        {
            try { action(); return Value.Null(); }
            catch (Exception error) { Report(error); return Value.String(error.Message); }
        }
        private static void Report(Exception error)
        {
            UI.Common.Toast.Present(error.Message);
            TeleporterPlugin.Active?.Context.Log.Error(error, "Teleporter action failed.");
        }
    }
}
