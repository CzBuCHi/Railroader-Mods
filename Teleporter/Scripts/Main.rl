from railroader import ui, WindowPosition
import teleporter as tp

var mainWindow = null
var editorWindow = null
var currentGroup = ""
var selectedLocation = ""
var newGroup = ""
var newLocation = ""
var creating = false
var editGroup = ""
var editName = ""
var editForGroup = ""
var editForName = ""
var confirmation = ""
var status = ""

func onWorldLoad(ctx) {
    mainWindow = ui.createWindow("czbuchi.teleporter.main", "Teleporter", buildMain)
        .size(392, 350).position(WindowPosition.lowerRight).resizable(300, 220, 1000, 1000)
    editorWindow = ui.createWindow("czbuchi.teleporter.editor", "Teleporter - Edit locations", buildEditor)
        .size(720, 480).position(WindowPosition.center).resizable(600, 360, 1400, 1000)
    ui.registerCommand("/teleporter", "Open saved camera views", (args) => { mainWindow.toggle() })
    if tp.autoOpen() { mainWindow.show() }
}

func onWorldUnload(ctx) {
    mainWindow?.close()
    editorWindow?.close()
    mainWindow = null
    editorWindow = null
}

func refresh() {
    mainWindow?.setNeedsRebuild()
    editorWindow?.setNeedsRebuild()
}

func openEditor() {
    creating = tp.groups().filter((group) => !tp.locations(group).isEmpty).isEmpty
    newGroup = currentGroup
    status = ""
    confirmation = ""
    editorWindow.show()
}

func buildMain(b) {
    tp.clipWindow(b)
    b.button("Edit locations...", openEditor).tooltip("Manage saved locations", "Select a location in the separate editor to change it.")
    var entries = tp.groups().filter((group) => !tp.locations(group).isEmpty)
    if entries.isEmpty {
        b.labelEmptyState("No saved locations. Open Edit locations to add one.")
        return
    }
    // One clipped scroll box: section headers are groups, icon/label rows are locations.
    tp.scrollView(b, (rows) => {
        for group in entries {
            rows.section(group)
            for name in tp.locations(group) {
                tp.locationRow(rows, name, group, name)
            }
        }
    })
}

// Temporary adapter. Replace only this function once Relay has an official text input API.
func textInput(builder, label, value, onChanged) {
    tp.inputField(builder, label, value, onChanged)
}

func completed(error, message) {
    if error != null { status = error } else { status = message }
    confirmation = ""
    refresh()
}

func buildEditor(b) {
    tp.clipWindow(b)
    b.button("New location", () => {
        creating = true
        newGroup = currentGroup
        newLocation = ""
        editForName = ""
        confirmation = ""
        status = ""
        refresh()
    })
    // Native list/detail remains useful for selecting and editing existing locations.
    tp.locationEditor(b, currentGroup, selectedLocation, buildEditorDetail, creating)
    if !creating {
        b.section("Preferences")
        b.fieldToggle("Open Teleporter on world load", () => tp.autoOpen(), (enabled) => {
            completed(tp.setAutoOpen(enabled), "Preference saved.")
        })
    }
}

func buildEditorDetail(b, group, name) {
    // Native list/detail creates an initial empty detail before applying its selection.
    if group == null && !creating {
        b.labelEmptyState("Select a location on the left, or click New location.")
        return
    }
    if group != null {
        if creating || currentGroup != group || selectedLocation != name {
            var rebuildEditor = creating
            currentGroup = group
            selectedLocation = name
            creating = false
            confirmation = ""
            status = ""
            if rebuildEditor { editorWindow?.setNeedsRebuild() }
        }
        if editForGroup != group || editForName != name {
            editGroup = group
            editName = name
            editForGroup = group
            editForName = name
        }
    }
    b.vScrollView((form) => {
        if creating {
            form.section("Add current view")
            textInput(form, "Group", newGroup, (value) => { newGroup = value })
            textInput(form, "Location name", newLocation, (value) => { newLocation = value })
            form.button("Add current view", () => {
                var error = tp.add(newGroup, newLocation)
                if error == null {
                    currentGroup = newGroup.trim()
                    selectedLocation = newLocation.trim()
                    newLocation = ""
                    creating = false
                }
                completed(error, "Current view saved.")
            })
        } else {
            form.section("Saved view")
            textInput(form, "Group", editGroup, (value) => { editGroup = value; confirmation = "" })
            textInput(form, "Location name", editName, (value) => { editName = value; confirmation = "" })
            form.button("Save name / group", () => {
                var error = tp.rename(currentGroup, selectedLocation, editGroup, editName)
                if error == null {
                    currentGroup = editGroup.trim()
                    selectedLocation = editName.trim()
                }
                completed(error, "Location name and group saved.")
            })
            form.button(confirmation == "replace" ? "Confirm replace" : "Replace with current view", () => {
                if confirmation != "replace" { confirmation = "replace"; refresh(); return }
                completed(tp.replace(currentGroup, selectedLocation), "Saved view replaced.")
            }).tooltip("Replace", "Click twice to overwrite this saved camera view.")
            form.button(confirmation == "delete" ? "Confirm delete" : "Delete", () => {
                if confirmation != "delete" { confirmation = "delete"; refresh(); return }
                completed(tp.remove(currentGroup, selectedLocation), "Saved view deleted.")
            }).tooltip("Delete", "Click twice to delete this saved location.")
            var groups = tp.groups()
            var index = groups.indexOf(currentGroup)
            form.hStack((order) => {
                order.button("Move group up", () => {
                    completed(tp.moveGroup(currentGroup, -1), "Group order saved.")
                }).disable(index == 0)
                order.button("Move group down", () => {
                    completed(tp.moveGroup(currentGroup, 1), "Group order saved.")
                }).disable(index == groups.count - 1)
            })
        }
        if status != "" { form.label(status) }
    })
}
