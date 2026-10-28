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
    tp.scrollView(b, (rows) => {
        var groups = tp.groups().filter((group) => !tp.locations(group).isEmpty)
        if groups.isEmpty { rows.label("No saved locations. Use the edit icon below to add one.") }
        for group in groups {
            rows.section(group)
            for name in tp.locations(group) { tp.locationRow(rows, name, group, name) }
        }
    })
    tp.editButton(b, openEditor)
}

func actionButton(builder, label, action) {
    return tp.button(builder, label, action)
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

func ensureSelection() {
    if creating { return }
    if tp.locations(currentGroup).contains(selectedLocation) { return }
    for group in tp.groups() {
        var names = tp.locations(group)
        if !names.isEmpty {
            currentGroup = group
            selectedLocation = names.first()
            return
        }
    }
    creating = true
    newGroup = currentGroup
    newLocation = ""
}

func buildEditor(b) {
    tp.clipWindow(b)
    ensureSelection()
    tp.actionBar(b, (bar) => {
        actionButton(bar, "New location", () => {
            creating = true
            newGroup = currentGroup
            newLocation = ""
            editForName = ""
            confirmation = ""
            status = ""
            editorWindow.setNeedsRebuild()
        }).width(120)
    })
    tp.editorColumns(b, buildLocationList, buildEditorForm)
    if !creating {
        b.section("Preferences")
        b.fieldToggle("Open Teleporter on world load", () => tp.autoOpen(), (enabled) => {
            completed(tp.setAutoOpen(enabled), "Preference saved.")
        })
    }
}

func buildLocationList(b) {
    tp.scrollView(b, (list) => {
        var groups = tp.groups().filter((group) => !tp.locations(group).isEmpty)
        if groups.isEmpty { list.label("No saved locations.") }
        for group in groups {
            list.section(group)
            for name in tp.locations(group) {
                tp.selectorRow(list, group, name, !creating && group == currentGroup && name == selectedLocation, () => {
                    currentGroup = group
                    selectedLocation = name
                    creating = false
                    confirmation = ""
                    status = ""
                    editorWindow.setNeedsRebuild()
                })
            }
        }
    })
}

func buildEditorForm(b) {
    if !creating && (editForGroup != currentGroup || editForName != selectedLocation) {
        editGroup = currentGroup
        editName = selectedLocation
        editForGroup = currentGroup
        editForName = selectedLocation
    }
    tp.scrollView(b, (form) => {
        if creating {
            form.section("Add current view")
            textInput(form, "Group", newGroup, (value) => { newGroup = value })
            textInput(form, "Location name", newLocation, (value) => { newLocation = value })
            actionButton(form, "Add current view", () => {
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
            actionButton(form, "Save name / group", () => {
                var error = tp.rename(currentGroup, selectedLocation, editGroup, editName)
                if error == null {
                    currentGroup = editGroup.trim()
                    selectedLocation = editName.trim()
                }
                completed(error, "Location name and group saved.")
            })
            actionButton(form, confirmation == "replace" ? "Confirm replace" : "Replace with current view", () => {
                if confirmation != "replace" { confirmation = "replace"; refresh(); return }
                completed(tp.replace(currentGroup, selectedLocation), "Saved view replaced.")
            }).tooltip("Replace", "Click twice to overwrite this saved camera view.")
            actionButton(form, confirmation == "delete" ? "Confirm delete" : "Delete", () => {
                if confirmation != "delete" { confirmation = "delete"; refresh(); return }
                completed(tp.remove(currentGroup, selectedLocation), "Saved view deleted.")
            }).tooltip("Delete", "Click twice to delete this saved location.")
            var groups = tp.groups()
            var index = groups.indexOf(currentGroup)
            form.hStack((order) => {
                actionButton(order, "Move group up", () => {
                    completed(tp.moveGroup(currentGroup, -1), "Group order saved.")
                }).disable(index == 0)
                actionButton(order, "Move group down", () => {
                    completed(tp.moveGroup(currentGroup, 1), "Group order saved.")
                }).disable(index == groups.count - 1)
            })
        }
        if status != "" { form.label(status) }
    })
}
