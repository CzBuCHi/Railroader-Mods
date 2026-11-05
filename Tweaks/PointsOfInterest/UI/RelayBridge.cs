using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System;
using Game.Scripting.Modding;
using Game.Scripting;
using HarmonyLib;
using Relay.Native;
using Relay.Types;
using Relay;
using TMPro;
using Tweaks.PointsOfInterest.Camera;
using Tweaks.PointsOfInterest;
using UI.Builder.Relay;
using UI.Builder;
using UI;
using UnityEngine.UI;
using UnityEngine;
using Value = Relay.Value;

namespace Tweaks.PointsOfInterest.UI
{
    [HarmonyPatch(typeof(ScriptInterpreterFactory), nameof(ScriptInterpreterFactory.CreatePackScopedBuilder))]
    internal static class RelayBridge
    {
        private static readonly FieldInfo BuilderField = typeof(RelayUIPanelBuilder).GetField("_builder",
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NotSupportedException("Relay builder field was changed.");
        private static readonly FieldInfo ContainerField = AccessTools.Field(typeof(UIPanelBuilder), "_container")
            ?? throw new NotSupportedException("Native panel container was changed.");

        internal static void Validate()
        {
            if (BuilderField.FieldType != typeof(UIPanelBuilder))
                throw new NotSupportedException("Unsupported Relay UI builder.");
            if (ContainerField.FieldType != typeof(RectTransform))
                throw new NotSupportedException("Unsupported native panel container.");
        }

        [HarmonyPostfix]
        private static void Postfix(string packIdentifier, ScriptCapabilityTier tier, ref IInterpreterBuilderFluent __result)
        {
            // Expose the native bridge only to this pack's full runtime scripts.
            if (packIdentifier == PointsOfInterestFeature.Identifier && tier == ScriptCapabilityTier.Full && PointsOfInterestFeature.Active != null)
                __result = __result.RegisterNamedModule(CreateModule());
        }

        internal static NativeModule CreateModule()
        {
            var module = new NativeModule("pointsOfInterest");
            module.DefineFunction("groups", 0, _ => Strings(Plugin.Store.Current.Groups.Select(group => group.Name)));
            module.DefineFunction("locations", 1, args => Strings(Plugin.Store.Current.Groups
                .FirstOrDefault(group => group.Name == args[0].AsString())?.Locations.Keys
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase) ?? Enumerable.Empty<string>()));
            module.DefineFunction("add", 2, args => Action(() => Plugin.Store.Add(args[0].AsString(), args[1].AsString(), CameraBookmarks.Capture())));
            module.DefineFunction("replace", 2, args => Action(() => Plugin.Store.Replace(args[0].AsString(), args[1].AsString(), CameraBookmarks.Capture())));
            module.DefineFunction("remove", 2, args => Action(() => Plugin.Store.Delete(args[0].AsString(), args[1].AsString())));
            module.DefineFunction("rename", 4, args => Action(() => Plugin.Store.Rename(args[0].AsString(), args[1].AsString(), args[2].AsString(), args[3].AsString())));
            module.DefineFunction("moveGroup", 2, args => Action(() => Plugin.Store.MoveGroup(args[0].AsString(), args[1].AsInt())));
            module.DefineFunction("autoOpen", 0, _ => Value.Bool(Plugin.Store.Current.AutoOpenWindow));
            module.DefineFunction("setAutoOpen", 1, args => Action(() => Plugin.Store.SetAutoOpen(args[0].AsBool())));
            module.DefineFunction("clipWindow", 1, args =>
            {
                var container = (RectTransform)ContainerField.GetValue(Builder(args[0]));
                if (container.GetComponent<RectMask2D>() == null) container.gameObject.AddComponent<RectMask2D>();
                ConfigureVertical(container);
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
                layout.minHeight = 0; layout.preferredHeight = 0; layout.flexibleHeight = 1;
                var content = scroll.content;
                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                ConfigureVertical(content);
                content.anchorMin = new Vector2(0, content.anchorMin.y);
                content.anchorMax = new Vector2(1, content.anchorMax.y);
                content.offsetMin = new Vector2(0, content.offsetMin.y);
                content.offsetMax = new Vector2(0, content.offsetMax.y);
                return Value.Null();
            });
            module.DefineFunction("editorColumns", 3, args =>
            {
                var context = Interpreter.CurrentCallContext;
                var left = args[1].AsClosure(); var right = args[2].AsClosure();
                var body = Builder(args[0]).HStack(columns =>
                {
                    var leftRoot = columns.VStack(panel => context.CallClosure(left, new[] { RelayUIPanelBuilder.Create(panel).Wrap() }));
                    ConfigureVertical(leftRoot);
                    var leftLayout = Flexible(leftRoot);
                    leftLayout.minWidth = 170; leftLayout.preferredWidth = 210; leftLayout.flexibleWidth = 0;
                    var rightRoot = columns.VStack(panel => context.CallClosure(right, new[] { RelayUIPanelBuilder.Create(panel).Wrap() }));
                    ConfigureVertical(rightRoot); Flexible(rightRoot);
                });
                var row = body.GetComponent<HorizontalLayoutGroup>();
                row.spacing = 12; row.childControlWidth = true; row.childForceExpandWidth = false;
                row.childControlHeight = true; row.childForceExpandHeight = true;
                Flexible(body);
                return Value.Null();
            });
            module.DefineFunction("actionBar", 2, args =>
            {
                var context = Interpreter.CurrentCallContext; var closure = args[1].AsClosure();
                var row = Builder(args[0]).ButtonStrip(bar => context.CallClosure(closure, new[] { RelayUIPanelBuilder.Create(bar).Wrap() }));
                ConfigureBar(row, false);
                return Value.Null();
            });
            module.DefineFunction("button", 3, args =>
            {
                var context = Interpreter.CurrentCallContext; var closure = args[2].AsClosure();
                var control = Builder(args[0]).AddButtonCompact(args[1].AsString(), () => Invoke(context, closure));
                FixedHeight(control.RectTransform, 28);
                return NativeConfigurableElement.Wrap(control);
            });
            module.DefineFunction("selectorRow", 5, args =>
            {
                var context = Interpreter.CurrentCallContext; var closure = args[4].AsClosure();
                var control = Builder(args[0]).AddButtonSelectable(args[2].AsString(), args[3].AsBool(), () => Invoke(context, closure));
                control.RectTransform.gameObject.name = "PointsOfInterest.Location." + LocationId(args[1].AsString(), args[2].AsString());
                FixedHeight(control.RectTransform, 25);
                var label = control.RectTransform.GetComponentInChildren<TMP_Text>();
                label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
                return Value.Null();
            });
            module.DefineFunction("editButton", 2, args =>
            {
                var context = Interpreter.CurrentCallContext; var closure = args[1].AsClosure();
                var footer = Builder(args[0]).ButtonStrip(bar =>
                {
                    IConfigurableElement? control = null;
                    foreach (var name in new[] { "Edit", "Pencil", "Customize" })
                        if (SpriteLibrary.TryGet(name, out _)) { control = bar.AddIconButton(name, () => Invoke(context, closure)); break; }
                    control = control ?? bar.AddButtonCompact(TextSprites.Copy, () => Invoke(context, closure));
                    FixedHeight(control.RectTransform, 28);
                    control.RectTransform.Square(28);
                    var layout = control.RectTransform.GetComponent<LayoutElement>();
                    layout.minWidth = 28; layout.preferredWidth = 28; layout.flexibleWidth = 0;
                    control.Tooltip("Edit points of interest", "Open the points of interest editor.");
                });
                ConfigureBar(footer, true);
                return Value.Null();
            });
            module.DefineFunction("spacer", 1, args =>
            {
                var space = Builder(args[0]).VStack(_ => { });
                Flexible(space);
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
                FixedHeight(row, 26);
                var layout = row.GetComponent<LayoutElement>();
                layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
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
                builder.FieldLabelWidth = 100;
                var field = builder.AddField(args[1].AsString(), input);
                FixedHeight(field.RectTransform, 30);
                return Value.Null();
            });
            return module;
        }

        private static string LocationId(string group, string name) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(group)) + ":" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name));

        private static PointsOfInterestFeature Plugin => PointsOfInterestFeature.RequireActive();
        private static void ConfigureVertical(RectTransform root)
        {
            var vertical = root.GetComponent<VerticalLayoutGroup>();
            if (vertical == null) return;
            vertical.childControlWidth = true; vertical.childForceExpandWidth = true;
            vertical.childControlHeight = true; vertical.childForceExpandHeight = false;
            vertical.spacing = 6;
        }
        private static LayoutElement Flexible(RectTransform root)
        {
            var layout = root.GetComponent<LayoutElement>() ?? root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
            layout.minHeight = 0; layout.preferredHeight = 0; layout.flexibleHeight = 1;
            return layout;
        }
        private static void FixedHeight(RectTransform root, float height)
        {
            var fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var layout = root.GetComponent<LayoutElement>() ?? root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height; layout.preferredHeight = height; layout.flexibleHeight = 0;
        }
        private static void ConfigureBar(RectTransform root, bool right)
        {
            FixedHeight(root, 32);
            var row = root.GetComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true; row.childForceExpandWidth = false;
            row.childControlHeight = true; row.childForceExpandHeight = false;
            row.childAlignment = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }
        private static void Invoke(Interpreter.CallContext context, Closure closure)
        {
            try { context.CallClosure(closure, Array.Empty<Value>()); }
            catch (Exception error) { Report(error); }
        }
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
            global::UI.Common.Toast.Present(error.Message);
            PointsOfInterestFeature.Active?.Context.Log.Error(error, "Points of interest action failed.");
        }
    }
}
