using System;
using System.Collections.Generic;
using System.Linq;
using Relay;
using Relay.Native;
using Relay.Types;
using Teleporter;
using Value = Relay.Value;

internal static class NativeWrap
{
    private static readonly Dictionary<object, ClassTypeDefinition> Types = new Dictionary<object, ClassTypeDefinition>();
    internal static Value Wrap<T>(this NativeClassDefinition<T> definition, T data) where T : class
    {
        if (!Types.TryGetValue(definition, out var type))
            Types.Add(definition, type = definition.CreateTypeDefinition());
        return Value.TypeInstance(new TypeInstance(type, data));
    }
}

internal static class RelayUiTests
{
    private sealed class Control
    {
        internal Action Click = () => { };
        internal bool Disabled;
    }
    private sealed class Panel
    {
        internal readonly Dictionary<string, Control> Buttons = new Dictionary<string, Control>();
        internal readonly Dictionary<string, Action<string>> Inputs = new Dictionary<string, Action<string>>();
        internal readonly Dictionary<string, Action<int>> Dropdowns = new Dictionary<string, Action<int>>();
        internal readonly Dictionary<string, Action<bool>> Toggles = new Dictionary<string, Action<bool>>();
        internal readonly List<string> Rows = new List<string>();
        internal readonly List<string> Labels = new List<string>();
        internal void Click(string name)
        {
            var control = Buttons[name];
            Program.Check(!control.Disabled, "Clicked button is enabled: " + name);
            control.Click();
        }
    }
    private sealed class Window
    {
        internal bool Open;
        internal bool Dirty;
        internal Panel Panel = new Panel();
        internal Action<Panel> Build = _ => { };
        internal void Show() { Open = true; Rebuild(); }
        internal void Rebuild() { Dirty = false; Panel = new Panel(); Build(Panel); }
    }
    private sealed class Ui
    {
        internal readonly Dictionary<string, Window> Windows = new Dictionary<string, Window>();
        internal void Flush()
        {
            for (var i = 0; i < 10; i++)
            {
                var dirty = Windows.Values.Where(window => window.Dirty && window.Open).ToArray();
                if (dirty.Length == 0) return;
                foreach (var window in dirty) window.Rebuild();
            }
            throw new Exception("Unexpected Relay rebuild loop.");
        }
    }

    private static readonly NativeClassDefinition<Control> ControlType = new NativeClassDefinition<Control>("Control")
        .Method("tooltip", (Control c, Value title, Value message) => ControlValue(c))
        .Method("width", (Control c, Value width) => ControlValue(c))
        .Method("disable", (Control c, Value disabled) => { c.Disabled = disabled.AsBool(); return ControlValue(c); });
    private static Value ControlValue(Control control) => ControlType.Wrap(control);
    private static Action Callback(Value value)
    {
        var context = Interpreter.CurrentCallContext;
        var closure = value.AsClosure();
        return () => context.CallClosure(closure, Array.Empty<Value>());
    }
    private static Action<Value> Callback1(Value value)
    {
        var context = Interpreter.CurrentCallContext;
        var closure = value.AsClosure();
        return arg => context.CallClosure(closure, new[] { arg });
    }
    private static readonly NativeClassDefinition<Panel> PanelType = CreatePanel();
    private static NativeClassDefinition<Panel> CreatePanel()
    {
        var definition = new NativeClassDefinition<Panel>("UIPanelBuilder");
        definition.Method("button", (Panel p, Value label, Value callback) =>
        {
            var control = new Control { Click = Callback(callback) };
            p.Buttons[label.AsString()] = control;
            return ControlValue(control);
        });
        foreach (var name in new[] { "vScrollView", "hStack" })
            definition.Method(name, (Panel p, Value callback) => { Callback1(callback)(PanelType.Wrap(p)); return Value.Null(); });
        foreach (var name in new[] { "section", "label", "labelEmptyState" })
            definition.Method(name, (Panel p, Value text) => { p.Labels.Add(text.AsString()); return Value.Null(); });
        definition.Method("fieldDropdown", (Panel p, Value label, Value options, Value current, Value callback) =>
        {
            var action = Callback1(callback);
            p.Dropdowns[label.AsString()] = index => action(Value.Integer(index));
            Program.Check(current.AsInt() >= 0 && current.AsInt() < options.AsArray().Count, "Dropdown has valid selection");
            return ControlValue(new Control());
        });
        definition.Method("fieldToggle", (Panel p, Value label, Value getter, Value callback) =>
        {
            Callback(getter)();
            var action = Callback1(callback);
            p.Toggles[label.AsString()] = enabled => action(Value.Bool(enabled));
            return ControlValue(new Control());
        });
        return definition;
    }
    private static readonly NativeClassDefinition<Window> WindowType = new NativeClassDefinition<Window>("WindowHandle")
        .Method("size", (Window w, Value width, Value height) => WindowType!.Wrap(w))
        .Method("position", (Window w, Value position) => WindowType!.Wrap(w))
        .Method("resizable", (Window w, Value minW, Value minH, Value maxW, Value maxH) => WindowType!.Wrap(w))
        .Method("show", (Window w) => { w.Show(); return Value.Null(); })
        .Method("toggle", (Window w) => { if (w.Open) w.Open = false; else w.Show(); return Value.Null(); })
        .Method("close", (Window w) => { w.Open = false; return Value.Null(); })
        .Method("setNeedsRebuild", (Window w) => { w.Dirty = true; return Value.Null(); });

    internal static void Run(string source, BookmarkStore store)
    {
        var ui = new Ui();
        var interpreter = new Interpreter();
        var uiType = new NativeClassDefinition<Ui>("UI")
            .Method("createWindow", (Ui owner, Value id, Value title, Value callback) =>
            {
                var build = Callback1(callback);
                var window = new Window { Build = panel => build(PanelType.Wrap(panel)) };
                owner.Windows.Add(id.AsString(), window);
                return WindowType.Wrap(window);
            })
            .Method("registerCommand", (Ui owner, Value name, Value description, Value callback) => Value.Null());
        var railroader = new NativeModule("railroader").DefineConstant("ui", uiType.Wrap(ui))
            .DefineEnum("WindowPosition", new Dictionary<string, Value> { ["center"] = Value.Integer(0), ["lowerRight"] = Value.Integer(1) });
        var tp = new NativeModule("teleporter");
        var captureX = 10f;
        var replaces = 0;
        var deletes = 0;
        Func<IEnumerable<string>, Value> strings = values => Value.Array(values.Select(Value.String).ToList());
        Func<Action, Value> action = callback =>
        {
            try { callback(); return Value.Null(); } catch (Exception error) { return Value.String(error.Message); }
        };
        tp.DefineFunction("groups", 0, _ => strings(store.Current.Groups.Select(group => group.Name)));
        tp.DefineFunction("locations", 1, args => strings(store.Current.Groups.Single(group => group.Name == args[0].AsString()).Locations.Keys.OrderBy(name => name)));
        tp.DefineFunction("autoOpen", 0, _ => Value.Bool(store.Current.AutoOpenTeleporterWindow));
        tp.DefineFunction("setAutoOpen", 1, args => action(() => store.SetAutoOpen(args[0].AsBool())));
        tp.DefineFunction("add", 2, args => action(() => store.Add(args[0].AsString(), args[1].AsString(), Program.View(captureX))));
        tp.DefineFunction("replace", 2, args => action(() => { replaces++; store.Replace(args[0].AsString(), args[1].AsString(), Program.View(captureX)); }));
        tp.DefineFunction("remove", 2, args => action(() => { deletes++; store.Delete(args[0].AsString(), args[1].AsString()); }));
        tp.DefineFunction("moveGroup", 2, args => action(() => store.MoveGroup(args[0].AsString(), args[1].AsInt())));
        tp.DefineFunction("locationRow", 4, args =>
        {
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            panel.Rows.Add(args[1].AsString());
            Program.Check(store.Get(args[2].AsString(), args[3].AsString()) != null, "Native row receives existing view IDs");
            return Value.Null();
        });
        tp.DefineFunction("inputField", 4, args =>
        {
            var callback = Callback1(args[3]);
            ((Panel)args[0].AsTypeInstance().NativeObject).Inputs[args[1].AsString()] = text => callback(Value.String(text));
            return Value.Null();
        });
        interpreter.RegisterNamedModule(railroader);
        interpreter.RegisterNamedModule(tp);
        interpreter.Interpret(source, "czbuchi/teleporter:Main.rl");
        interpreter.CallFunction("onWorldLoad", Value.Null());
        var main = ui.Windows["czbuchi.teleporter.main"];
        var editor = ui.Windows["czbuchi.teleporter.editor"];
        Program.Check(!main.Open && !editor.Open, "Windows initially hidden");
        main.Show();
        Program.Check(main.Panel.Buttons.Keys.SequenceEqual(new[] { "Edit locations..." }), "Main window contains no edit actions");
        main.Panel.Click("Edit locations...");
        Program.Check(editor.Open && editor.Panel.Inputs.Count == 2, "Editor has two input adapters");
        editor.Panel.Inputs["Group"]("Yard");
        editor.Panel.Inputs["Location name"]("Saved view");
        editor.Panel.Click("Add current view"); ui.Flush();
        Program.Check(store.Get("Yard", "Saved view").Position.X == 10, "Relay add uses latest typed names");
        Program.Check(main.Panel.Rows.SequenceEqual(new[] { "Saved view" }), "Main has native icon-label row");
        captureX = 20;
        editor.Panel.Click("Replace with current view"); ui.Flush();
        Program.Check(replaces == 0, "First replace click only confirms");
        editor.Panel.Click("Confirm replace"); ui.Flush();
        Program.Check(replaces == 1 && store.Get("Yard", "Saved view").Position.X == 20, "Second replace click stores current view");
        editor.Panel.Click("Add current view"); ui.Flush();
        Program.Check(store.Current.Groups[0].Locations.Count == 1, "Invalid add does not create a view");
        editor.Panel.Click("Delete"); ui.Flush();
        Program.Check(deletes == 0, "First delete click only confirms");
        editor.Panel.Click("Confirm delete"); ui.Flush();
        Program.Check(deletes == 1 && main.Panel.Rows.Count == 0, "Confirmed deletion refreshes main");
        editor.Panel.Toggles["Open Teleporter on world load"](true); ui.Flush();
        Program.Check(store.Current.AutoOpenTeleporterWindow, "Editor preference saves through native API");
        interpreter.CallFunction("onWorldUnload", Value.Null());
        Program.Check(!main.Open && !editor.Open, "World unload closes both windows");
        ui.Windows.Clear();
        interpreter.CallFunction("onWorldLoad", Value.Null());
        Program.Check(ui.Windows["czbuchi.teleporter.main"].Open, "Auto-open preference honored on next world");
    }
}
