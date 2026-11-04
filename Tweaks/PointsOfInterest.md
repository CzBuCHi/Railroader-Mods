# Points of interest

Saved camera views are included in Tweaks 3.0.14 for Railroader 0.6.x.

## Main window

Open **Points of interest** with **Alt+T**, or `/poi` in the game console. Change the shortcut in the vanilla **Bindings > Camera** tab: **Points of interest** is the last entry. Native conflict detection, chord rebinding, saving, and Reset work as for other game actions. Reset restores Alt+T; either Alt key works. The toolbar portal icon has been removed.

Install the Tweaks package, including its DLL and `Scripts/Main.rl`. Disable/remove the standalone Teleporter mod before enabling Tweaks to avoid duplicate windows and commands.

All saved locations appear together in one scroll box: section headings show group names, followed by location rows with the native destination icon and location label. Click a destination icon to restore its saved view, including repeated clicks on the same icon. No group dropdown, selection highlight, or right-hand detail column is present.

The scroll box stretches to the available width and clips its content to its viewport. Long location labels use ellipsis and keep their full name in the tooltip. Both windows also clip content at the window content boundary.

Groups follow the saved group order; locations are sorted by name within each group. Empty groups are omitted from the list. Adding, replacing, deleting, and reordering are kept out of this window.

The icon-only **Edit points of interest** button in the bottom-right corner opens a separate editor. Its tooltip identifies the action. The icon uses the game's edit/pencil/customize sprite when available, with its native Copy glyph as a fallback. Both windows can be moved and resized.

## Editor

- The editor has two independent columns: a grouped selection list on the left and the selected location's form on the right. Each column scrolls on its own. The old list/detail component is no longer used. Selecting a location does not teleport the camera.
- Click **New location** to display the add form on the right. Enter group/name, then **Add current view**. A new group is created if needed; an existing name is never silently overwritten.
- Select a saved location to replace its view or delete it. Both actions require a second click to confirm; selecting another location cancels pending confirmation.
- Change the Group and Location name fields and click **Save name / group** to rename a saved location or move it into another group. The saved position, orientation, zoom, and camera mode remain unchanged. A destination-name collision is rejected without overwriting either view.
- Move the selected location's group up/down using the controls on the right.
- **Preferences**, including Open Points of interest on world load, is below both columns. It is hidden while adding a new location.

The main window and editor refresh after successful changes. Text fields update their draft values while typing. The editor stores nothing until an action is confirmed.

## Camera bookmarks

First-person bookmarks restore player position and viewing direction. Tracking/overhead bookmarks restore the absolute target position, pitch, yaw, and zoom distance. The appropriate camera mode is selected automatically. Tracking a train when saving still creates a fixed bookmark at that location, not a moving train-relative bookmark.

Teleportation requests terrain loading before restoring the camera. Native loading progress appears only if the load takes long enough. Repeated jumps cancel the previous pending jump. World unload and mod disable cancel loading and close both windows.

## Saved data

Local file, shared between maps:

`Documents\Railroader\ModSettings\czbuchi\teleporter.json`

Tweaks retains the existing Teleporter profile at this path. It does not scan, import, or depend on RailForge configuration.

Create new bookmarks in the current map; old RailForge coordinates are not imported.

Edits write immediately using atomic replacement; `.bak` holds the previous file. A failed write does not commit in-memory changes. Invalid profiles stop Tweaks initialization and are not silently replaced with empty data. Native profile files use UTF-8 with BOM and CRLF.

## Editing the UI / future Relay input support

Both windows and their interaction flow are implemented in `Scripts/Main.rl`. Change this file to adjust the UI; restart the game to reload the pack. DLL changes always require a restart too.

The DLL patches `ScriptInterpreterFactory.CreatePackScopedBuilder` with a Harmony postfix to register a native module named `pointsOfInterest` only for this pack's full runtime scripts. It does not register it globally or expose it to Data.rl.

The `textInput(...)` helper in Main.rl is the temporary text-input adapter. It calls `tp.inputField(...)`, which unwraps the native panel builder and adds the game's standard text field. Once an official Relay input API is released, update this helper to its documented signature; no future API shape is assumed and no built-in Relay method is overridden.

Main.rl owns grouping, selection, and forms. `tp.scrollView(...)` adds a clipped native vertical scroll view; `tp.locationRow(...)` adds a destination-icon/label row. `tp.clipWindow(...)` clips the root and disables forced vertical expansion of ordinary controls. `tp.editorColumns(...)` creates a horizontal container with a 210-pixel preferred left column (170-pixel minimum) and a flexible right column; both host their own scroll views. Only these content areas expand vertically. Buttons, inputs, rows, and header/footer bars have explicit heights. `tp.editButton(...)` adds the icon-only, right-aligned main footer. `tp.button(...)` and `tp.actionBar(...)` create fixed-height form/header actions. There is no dependency on ListDetailController or its private fields.

Selection callbacks retain both group and location names so identical names in different groups remain distinct. `tp.rename(...)` moves/renames the stored view without capturing the current camera. Other bridge functions handle lists, capture for add/replace, deletion, ordering, and auto-open. The DLL uses native strategy camera state capture/restore, validated reflection for camera-mode selection and inertia reset, and no camera update transpilers.

## Validation

Run the bookmark and real Relay VM UI suite against the merged Tweaks assembly:

```powershell
dotnet run --project tests/PointsOfInterest.Tests -c Release -- Tweaks/Scripts/Main.rl 'C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed' Tweaks/bin/Release/CzBuCHi.Tweaks.dll
```

Tests check saved data, UI actions and titles, default chord definition, installed input APIs, and the absence of the toolbar icon. They do not run Unity. In game, check Alt+T, rebinding to a new chord, persistence after restart, Reset to Alt+T, Camera tab ordering, typing/rebind focus, world unload, mod disable/re-enable, and first-person/tracking bookmark restoration.
