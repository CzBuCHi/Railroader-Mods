from railroader import ui, WindowPosition
import teleporter as tp

var mainWindow = null
var editorWindow = null
var currentGroup = ""
var selectedLocation = ""
var newGroup = ""
var newLocation = ""
var creating = false
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
    b.button("Edit locations...", openEditor).tooltip("Manage saved locations", "Select a location in the separate editor to change it.")
    var entries = tp.groups().filter((group) => !tp.locations(group).isEmpty)
    if entries.isEmpty {
        b.labelEmptyState("No saved locations. Open Edit locations to add one.")
        return
    }
    // Native purchase-style grouped list. Its detail column is hidden by the bridge.
    tp.locationBrowser(b)
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
    b.button("New location", () => {
        creating = true
        newGroup = currentGroup
        newLocation = ""
        confirmation = ""
        status = ""
        refresh()
    })
    // Same native list as the main window, with the right column enabled for editing.
    tp.locationEditor(b, currentGroup, selectedLocation, buildEditorDetail, creating)
}

func buildEditorDetail(b, group, name) {
    // Native list/detail creates an initial empty detail before applying its selection.
    if group == null && !creating {
        b.labelEmptyState("Select a location on the left, or click New location.")
        return
    }
    if group != null {
        if creating || currentGroup != group || selectedLocation != name {
            currentGroup = group
            selectedLocation = name
            creating = false
            confirmation = ""
            status = ""
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
            form.field("Group", currentGroup)
            form.field("Location", selectedLocation)
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
        form.section("Preferences")
        form.fieldToggle("Open Teleporter on world load", () => tp.autoOpen(), (enabled) => {
            completed(tp.setAutoOpen(enabled), "Preference saved.")
        })
        if status != "" { form.label(status) }
    })
}
