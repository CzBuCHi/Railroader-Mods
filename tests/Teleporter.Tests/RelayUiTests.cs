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
        internal readonly Dictionary<string, string> InputValues = new Dictionary<string, string>();
        internal readonly Dictionary<string, Action<int>> Dropdowns = new Dictionary<string, Action<int>>();
        internal readonly Dictionary<string, Action<bool>> Toggles = new Dictionary<string, Action<bool>>();
        internal readonly List<string> Rows = new List<string>();
        internal readonly List<string> Labels = new List<string>();
        internal readonly List<string> Content = new List<string>();
        internal readonly Dictionary<string, Action> Selections = new Dictionary<string, Action>();
        internal Panel? Detail;
        internal bool Clipped;
        internal int ScrollBoxes;
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
        definition.Method("section", (Panel p, Value text) => { p.Content.Add("group:" + text.AsString()); return Value.Null(); });
        definition.Method("field", (Panel p, Value label, Value text) => { p.Labels.Add(label.AsString() + ":" + text.AsString()); return ControlValue(new Control()); });
        foreach (var name in new[] { "label", "labelEmptyState" })
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
        tp.DefineFunction("rename", 4, args => action(() => store.Rename(args[0].AsString(), args[1].AsString(), args[2].AsString(), args[3].AsString())));
        tp.DefineFunction("moveGroup", 2, args => action(() => store.MoveGroup(args[0].AsString(), args[1].AsInt())));
        var browserClicks = new List<string>();
        Action<Panel, List<BookmarkCatalog.Entry>> catalog = (panel, entries) =>
        {
            string? previousGroup = null;
            foreach (var entry in entries)
            {
                if (entry.Group != previousGroup) panel.Content.Add("group:" + entry.Group);
                previousGroup = entry.Group;
                panel.Content.Add("row:" + entry.Group + ":" + entry.Name);
                panel.Rows.Add(entry.Name);
            }
        };
        tp.DefineFunction("clipWindow", 1, args =>
        {
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            panel.Clipped = true;
            return Value.Null();
        });
        tp.DefineFunction("scrollView", 2, args =>
        {
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            panel.ScrollBoxes++;
            Callback1(args[1])(PanelType.Wrap(panel));
            return Value.Null();
        });
        tp.DefineFunction("locationRow", 4, args =>
        {
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            var entry = new BookmarkCatalog.Entry(args[2].AsString(), args[3].AsString());
            Program.Check(store.Get(entry.Group, entry.Name) != null, "Icon row targets an existing saved view");
            panel.Rows.Add(args[1].AsString());
            panel.Content.Add("row:" + entry.Group + ":" + entry.Name);
            panel.Selections[entry.Id] = () => browserClicks.Add(entry.Group + ":" + entry.Name);
            return Value.Null();
        });
        tp.DefineFunction("locationEditor", 5, args =>
        {
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            var entries = BookmarkCatalog.Create(store.Current);
            catalog(panel, entries);
            var context = Interpreter.CurrentCallContext;
            var closure = args[3].AsClosure();
            Action<BookmarkCatalog.Entry?> select = entry =>
            {
                panel.Detail = new Panel();
                context.CallClosure(closure, new[] {
                    PanelType.Wrap(panel.Detail), entry == null ? Value.Null() : Value.String(entry.Group),
                    entry == null ? Value.Null() : Value.String(entry.Name)
                });
            };
            foreach (var entry in entries) panel.Selections[entry.Id] = () => select(entry);
            select(null);
            if (!args[4].AsBool())
            {
                var selected = entries.FirstOrDefault(entry => entry.Group == args[1].AsString() && entry.Name == args[2].AsString())
                    ?? entries.FirstOrDefault();
                if (selected != null) select(selected);
            }
            return Value.Null();
        });
        tp.DefineFunction("inputField", 4, args =>
        {
            var callback = Callback1(args[3]);
            var panel = (Panel)args[0].AsTypeInstance().NativeObject;
            var label = args[1].AsString();
            panel.InputValues[label] = args[2].AsString();
            panel.Inputs[label] = text => { panel.InputValues[label] = text; callback(Value.String(text)); };
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
        Program.Check(editor.Open && editor.Panel.Detail!.Inputs.Count == 2, "Editor opens new-location form on the right when empty");
        Program.Check(!editor.Panel.Toggles.Any() && !editor.Panel.Detail!.Toggles.Any(), "Preferences hidden during creation");
        editor.Panel.Detail!.Inputs["Group"]("Yard");
        editor.Panel.Detail.Inputs["Location name"]("Saved view");
        editor.Panel.Detail.Click("Add current view"); ui.Flush();
        Program.Check(store.Get("Yard", "Saved view").Position.X == 10, "Relay add uses latest typed names");
        Program.Check(main.Panel.Rows.SequenceEqual(new[] { "Saved view" }), "Main has grouped location list");
        Program.Check(editor.Panel.Detail!.InputValues["Location name"] == "Saved view", "Newly added view has editable name");
        Program.Check(editor.Panel.Toggles.ContainsKey("Open Teleporter on world load") && !editor.Panel.Detail.Toggles.Any(),
            "Preferences are below the split list, outside its right detail");
        captureX = 20;
        editor.Panel.Detail.Click("Replace with current view"); ui.Flush();
        Program.Check(replaces == 0, "First replace click only confirms");
        editor.Panel.Detail!.Click("Confirm replace"); ui.Flush();
        Program.Check(replaces == 1 && store.Get("Yard", "Saved view").Position.X == 20, "Second replace click stores current view");
        editor.Panel.Click("New location"); ui.Flush();
        Program.Check(!editor.Panel.Toggles.Any() && !editor.Panel.Detail!.Toggles.Any(), "New mode removes editor preference footer");
        editor.Panel.Detail!.Inputs["Group"]("Yard");
        editor.Panel.Detail.Inputs["Location name"]("Saved view");
        editor.Panel.Detail.Click("Add current view"); ui.Flush();
        Program.Check(store.Current.Groups[0].Locations.Count == 1, "Duplicate add does not overwrite saved view");
        editor.Panel.Selections[BookmarkCatalog.Create(store.Current).Single().Id]();
        editor.Panel.Detail!.Click("Delete"); ui.Flush();
        Program.Check(deletes == 0, "First delete click only confirms");
        editor.Panel.Detail!.Click("Confirm delete"); ui.Flush();
        Program.Check(deletes == 1 && main.Panel.Rows.Count == 0, "Confirmed deletion refreshes main");
        store.Add("Yard", "Beta", Program.View());
        store.Add("Yard", "Alpha", Program.View());
        store.Add("Station", "Alpha", Program.View(50));
        store.Add("Empty", "Temporary", Program.View());
        store.Delete("Empty", "Temporary");
        main.Rebuild(); editor.Rebuild();
        Program.Check(main.Panel.Dropdowns.Count == 0 && editor.Panel.Dropdowns.Count == 0, "Neither window requires dropdown selection");
        Program.Check(main.Panel.Rows.SequenceEqual(new[] { "Alpha", "Beta", "Alpha" }), "All groups' locations are visible together");
        Program.Check(main.Panel.Content.SequenceEqual(new[] { "group:Yard", "row:Yard:Alpha", "row:Yard:Beta", "group:Station", "row:Station:Alpha" }),
            "Group headers precede sorted locations and duplicate names retain the correct group");
        Program.Check(main.Panel.Detail == null, "Main window has no detail column");
        Program.Check(editor.Panel.Rows.SequenceEqual(main.Panel.Rows) && editor.Panel.Detail != null, "Editor lists the same locations with a right detail column");
        Program.Check(main.Panel.Clipped && main.Panel.ScrollBoxes == 1 && editor.Panel.Clipped,
            "Main uses one clipped scroll box and both window roots request clipping");
        var stationId = BookmarkCatalog.Create(store.Current).Single(entry => entry.Group == "Station").Id;
        main.Panel.Selections[stationId](); main.Panel.Selections[stationId]();
        Program.Check(browserClicks.SequenceEqual(new[] { "Station:Alpha", "Station:Alpha" }), "Same main row can teleport repeatedly with one click");
        var jumpsBefore = browserClicks.Count;
        editor.Panel.Selections[stationId]();
        Program.Check(editor.Panel.Detail!.InputValues["Group"] == "Station" && browserClicks.Count == jumpsBefore,
            "Editor selection chooses the correct duplicate-named view without teleporting");
        editor.Panel.Detail.Click("Delete"); ui.Flush();
        var yardId = BookmarkCatalog.Create(store.Current).First(entry => entry.Group == "Yard").Id;
        editor.Panel.Selections[yardId]();
        Program.Check(editor.Panel.Detail!.Buttons.ContainsKey("Delete") && !editor.Panel.Detail.Buttons.ContainsKey("Confirm delete"),
            "Selecting a different location cancels pending confirmation");
        editor.Panel.Detail.Inputs["Group"]("New group");
        editor.Panel.Detail.Inputs["Location name"]("Renamed Alpha");
        var originalX = store.Get("Yard", "Alpha").Position.X;
        editor.Panel.Detail.Click("Save name / group"); ui.Flush();
        Program.Check(store.Get("New group", "Renamed Alpha").Position.X == originalX && replaces == 1,
            "Rename/move preserves the original camera instead of capturing current view");
        Program.Check(editor.Panel.Detail!.InputValues["Location name"] == "Renamed Alpha" && main.Panel.Rows.Contains("Renamed Alpha"),
            "Rename updates selected detail and main list");
        editor.Panel.Detail.Inputs["Group"]("Station");
        editor.Panel.Detail.Inputs["Location name"]("Alpha");
        editor.Panel.Detail.Click("Save name / group"); ui.Flush();
        Program.Check(store.Get("New group", "Renamed Alpha").Position.X == originalX && store.Get("Station", "Alpha").Position.X == 50,
            "Name collision does not replace either view");
        store.MoveGroup("Station", -1);
        main.Rebuild();
        Program.Check(main.Panel.Content.First() == "group:Station", "Main honors saved group order");
        editor.Panel.Toggles["Open Teleporter on world load"](true); ui.Flush();
        Program.Check(store.Current.AutoOpenTeleporterWindow, "Editor preference saves through native API");
        interpreter.CallFunction("onWorldUnload", Value.Null());
        Program.Check(!main.Open && !editor.Open, "World unload closes both windows");
        ui.Windows.Clear();
        interpreter.CallFunction("onWorldLoad", Value.Null());
        Program.Check(ui.Windows["czbuchi.teleporter.main"].Open, "Auto-open preference honored on next world");
    }
}
