from railroader import ui, WindowPosition
import teleporter as tp

var mainWindow = null
var editorWindow = null
var currentGroup = ""
var selectedLocation = ""
var newGroup = ""
var newLocation = ""
var confirmation = ""
var status = ""

func onWorldLoad(ctx) {
    mainWindow = ui.createWindow("czbuchi.teleporter.main", "Teleporter", buildMain)
        .size(392, 350).position(WindowPosition.lowerRight).resizable(300, 220, 1000, 1000)
    editorWindow = ui.createWindow("czbuchi.teleporter.editor", "Teleporter - Edit locations", buildEditor)
        .size(480, 500).position(WindowPosition.center).resizable(380, 420, 1000, 1000)
    ui.registerCommand("/teleporter", "Open saved camera views", (args) => { mainWindow.toggle() })
    if tp.autoOpen() { mainWindow.show() }
}

func onWorldUnload(ctx) {
    mainWindow?.close()
    editorWindow?.close()
    mainWindow = null
    editorWindow = null
}

func selectedGroup(groups) {
    if !groups.contains(currentGroup) {
        currentGroup = groups.first() ?? ""
        selectedLocation = ""
    }
    return groups.indexOf(currentGroup)
}

func refresh() {
    mainWindow?.setNeedsRebuild()
    editorWindow?.setNeedsRebuild()
}

func openEditor() {
    newGroup = currentGroup
    status = ""
    confirmation = ""
    editorWindow.show()
}

func buildMain(b) {
    var groups = tp.groups()
    var index = selectedGroup(groups)
    b.button("Edit locations...", openEditor).tooltip("Manage saved locations", "Add, replace, delete, or reorder in a separate window.")
    if groups.isEmpty {
        b.labelEmptyState("No saved locations. Open Edit locations to add one.")
        return
    }
    b.fieldDropdown("Group", groups, index, (value) => {
        currentGroup = groups[value]
        selectedLocation = ""
        confirmation = ""
        refresh()
    })
    var names = tp.locations(currentGroup)
    if names.isEmpty {
        b.labelEmptyState("No locations in this group.")
        return
    }
    b.vScrollView((rows) => {
        for name in names {
            // Native wagon location row: clickable destination icon followed by its label.
            tp.locationRow(rows, name, currentGroup, name)
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
    var groups = tp.groups()
    var index = selectedGroup(groups)
    b.vScrollView((form) => {
        form.section("Add current view")
        textInput(form, "Group", newGroup, (value) => { newGroup = value })
        textInput(form, "Location name", newLocation, (value) => { newLocation = value })
        form.button("Add current view", () => {
            var error = tp.add(newGroup, newLocation)
            if error == null {
                currentGroup = newGroup.trim()
                selectedLocation = newLocation.trim()
                newLocation = ""
            }
            completed(error, "Current view saved.")
        })
        form.section("Manage saved views")
        if !groups.isEmpty {
            form.fieldDropdown("Group", groups, index, (value) => {
                currentGroup = groups[value]
                newGroup = currentGroup
                selectedLocation = ""
                confirmation = ""
                refresh()
            })
            var names = tp.locations(currentGroup)
            if !names.contains(selectedLocation) { selectedLocation = names.first() ?? "" }
            if !names.isEmpty {
                form.fieldDropdown("Location", names, names.indexOf(selectedLocation), (value) => {
                    selectedLocation = names[value]
                    confirmation = ""
                    refresh()
                })
                form.hStack((actions) => {
                    actions.button(confirmation == "replace" ? "Confirm replace" : "Replace with current view", () => {
                        if confirmation != "replace" { confirmation = "replace"; refresh(); return }
                        completed(tp.replace(currentGroup, selectedLocation), "Saved view replaced.")
                    }).tooltip("Replace", "Click twice to overwrite this saved camera view.")
                    actions.button(confirmation == "delete" ? "Confirm delete" : "Delete", () => {
                        if confirmation != "delete" { confirmation = "delete"; refresh(); return }
                        completed(tp.remove(currentGroup, selectedLocation), "Saved view deleted.")
                    }).tooltip("Delete", "Click twice to delete this saved location.")
                })
            } else { form.label("This group has no saved locations.") }
            form.hStack((order) => {
                order.button("Move group up", () => {
                    completed(tp.moveGroup(currentGroup, -1), "Group order saved.")
                }).disable(index == 0)
                order.button("Move group down", () => {
                    completed(tp.moveGroup(currentGroup, 1), "Group order saved.")
                }).disable(index == groups.count - 1)
            })
        } else { form.label("No saved locations yet.") }
        form.section("Preferences")
        form.fieldToggle("Open Teleporter on world load", () => tp.autoOpen(), (enabled) => {
            completed(tp.setAutoOpen(enabled), "Preference saved.")
        })
        if status != "" { form.label(status) }
    })
}
