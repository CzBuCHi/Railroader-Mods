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
using UI;
using UnityEngine;
using UnityEngine.UI;
using Value = Relay.Value;

namespace Teleporter
{
    [HarmonyPatch(typeof(ScriptInterpreterFactory), nameof(ScriptInterpreterFactory.CreatePackScopedBuilder))]
    internal static class RelayBridge
    {
        private static readonly FieldInfo BuilderField = typeof(RelayUIPanelBuilder).GetField("_builder",
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NotSupportedException("Relay builder field was changed.");
        private static readonly FieldInfo ListField = AccessTools.Field(typeof(ListDetailController), "listController")
            ?? throw new NotSupportedException("Native list controller was changed.");
        private static readonly FieldInfo DetailField = AccessTools.Field(typeof(ListDetailController), "detailRectTransform")
            ?? throw new NotSupportedException("Native detail panel was changed.");
        private static readonly FieldInfo ContainerField = AccessTools.Field(typeof(UIPanelBuilder), "_container")
            ?? throw new NotSupportedException("Native panel container was changed.");

        internal static void Validate()
        {
            if (BuilderField.FieldType != typeof(UIPanelBuilder))
                throw new NotSupportedException("Unsupported Relay UI builder.");
            if (ListField.FieldType != typeof(ListController) || DetailField.FieldType != typeof(RectTransform))
                throw new NotSupportedException("Unsupported native grouped list.");
            if (ContainerField.FieldType != typeof(RectTransform))
                throw new NotSupportedException("Unsupported native panel container.");
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
            module.DefineFunction("rename", 4, args => Action(() => Plugin.Store.Rename(args[0].AsString(), args[1].AsString(), args[2].AsString(), args[3].AsString())));
            module.DefineFunction("moveGroup", 2, args => Action(() => Plugin.Store.MoveGroup(args[0].AsString(), args[1].AsInt())));
            module.DefineFunction("autoOpen", 0, _ => Value.Bool(Plugin.Store.Current.AutoOpenTeleporterWindow));
            module.DefineFunction("setAutoOpen", 1, args => Action(() => Plugin.Store.SetAutoOpen(args[0].AsBool())));
            module.DefineFunction("clipWindow", 1, args =>
            {
                var container = (RectTransform)ContainerField.GetValue(Builder(args[0]));
                if (container.GetComponent<RectMask2D>() == null) container.gameObject.AddComponent<RectMask2D>();
                return Value.Null();
            });
            module.DefineFunction("scrollView", 2, args =>
            {
                var context = Interpreter.CurrentCallContext;
                var closure = args[1].AsClosure();
                var root = Builder(args[0]).VScrollView(rows =>
                    context.CallClosure(closure, new[] { RelayUIPanelBuilder.Create(rows).Wrap() }));
                var scroll = root.GetComponent<ScrollRect>();
                scroll.horizontal = false;
                var viewport = scroll.viewport;
                if (viewport != null && viewport.GetComponent<RectMask2D>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
                var layout = root.GetComponent<LayoutElement>();
                layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
                var content = scroll.content;
                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                var vertical = content.GetComponent<VerticalLayoutGroup>();
                if (vertical != null) { vertical.childControlWidth = true; vertical.childForceExpandWidth = true; }
                content.anchorMin = new Vector2(0, content.anchorMin.y);
                content.anchorMax = new Vector2(1, content.anchorMax.y);
                content.offsetMin = new Vector2(0, content.offsetMin.y);
                content.offsetMax = new Vector2(0, content.offsetMax.y);
                return Value.Null();
            });
            module.DefineFunction("locationEditor", 5, args =>
            {
                var entries = BookmarkCatalog.Create(Plugin.Store.Current);
                var selected = entries.FirstOrDefault(entry => entry.Group == args[1].AsString() && entry.Name == args[2].AsString())
                    ?? entries.FirstOrDefault();
                var state = new UIState<string>(args[4].AsBool() ? "" : selected?.Id ?? "");
                var context = Interpreter.CurrentCallContext;
                var closure = args[3].AsClosure();
                var view = Builder(args[0]).AddListDetail(Items(entries), state, (detail, entry) =>
                {
                    try
                    {
                        context.CallClosure(closure, new[] {
                            RelayUIPanelBuilder.Create(detail).Wrap(),
                            entry == null ? Value.Null() : Value.String(entry.Group),
                            entry == null ? Value.Null() : Value.String(entry.Name)
                        });
                    }
                    catch (Exception error) { Report(error); }
                }, listWidth: 220f);
                // Let the list share available height with the Preferences footer below it.
                var layout = view.GetComponent<LayoutElement>() ?? view.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
                layout.minHeight = 0; layout.preferredHeight = 0; layout.flexibleHeight = 1;
                return Value.Null();
            });
            module.DefineFunction("locationRow", 4, args =>
            {
                var builder = Builder(args[0]);
                var group = args[2].AsString(); var name = args[3].AsString();
                var row = builder.AddLocationFieldFallback(args[1].AsString(), () =>
                {
                    try { Plugin.Runtime.Teleport(Plugin.Store.Get(group, name)); }
                    catch (Exception error) { Report(error); }
                });
                var rowLayout = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
                rowLayout.minWidth = 0; rowLayout.preferredWidth = 0; rowLayout.flexibleWidth = 1;
                var label = row.GetComponentInChildren<TMP_Text>();
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                var textLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
                textLayout.minWidth = 0; textLayout.preferredWidth = 0; textLayout.flexibleWidth = 1;
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
        private static IEnumerable<UIPanelBuilder.ListItem<BookmarkCatalog.Entry>> Items(IEnumerable<BookmarkCatalog.Entry> entries) =>
            entries.Select(entry => new UIPanelBuilder.ListItem<BookmarkCatalog.Entry>(entry.Id, entry, entry.Group, entry.Name));
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
