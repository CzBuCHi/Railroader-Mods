# Teleporter 3.0.5 - Relay UI + native DLL

For Railroader 0.6.x. Identifier: `czbuchi/teleporter`.

## Install

1. Close the game and disable the old Teleporter so only one version runs.
2. Extract the ZIP's `CzBuCHi.Teleporter` folder into `Documents\Railroader\Mods`.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods**, restart, then enable **Teleporter** in Mods and click Apply.

Keep the DLL and `Scripts/Main.rl` together. RailForge and UMM are not required. The mod uses the `0Harmony.dll` already supplied in the game's Managed directory; the ZIP does not distribute game libraries.

## Main window

Open Teleporter using its portal icon in the top-right toolbar, or `/teleporter` in the game console.

All saved locations appear together in one scroll box: section headings show group names, followed by location rows with the native destination icon and location label. Click a destination icon to restore its saved view, including repeated clicks on the same icon. No group dropdown, selection highlight, or right-hand detail column is present.

The scroll box stretches to the available width and clips its content to its viewport. Long location labels use ellipsis and keep their full name in the tooltip. Both windows also clip content at the window content boundary.

Groups follow the saved group order; locations are sorted by name within each group. Empty groups are omitted from the list. Adding, replacing, deleting, and reordering are kept out of this window.

The icon-only **Edit locations** button in the bottom-right corner opens a separate editor. Its tooltip identifies the action. The icon uses the game's edit/pencil/customize sprite when available, with its native Copy glyph as a fallback. Both windows can be moved and resized.

## Editor

- The editor has two independent columns: a grouped selection list on the left and the selected location's form on the right. Each column scrolls on its own. The old list/detail component is no longer used. Selecting a location does not teleport the camera.
- Click **New location** to display the add form on the right. Enter group/name, then **Add current view**. A new group is created if needed; an existing name is never silently overwritten.
- Select a saved location to replace its view or delete it. Both actions require a second click to confirm; selecting another location cancels pending confirmation.
- Change the Group and Location name fields and click **Save name / group** to rename a saved location or move it into another group. The saved position, orientation, zoom, and camera mode remain unchanged. A destination-name collision is rejected without overwriting either view.
- Move the selected location's group up/down using the controls on the right.
- **Preferences**, including Open Teleporter on world load, is below both columns. It is hidden while adding a new location.

The main window and editor refresh after successful changes. Text fields update their draft values while typing. The editor stores nothing until an action is confirmed.

## Camera bookmarks

First-person bookmarks restore player position and viewing direction. Tracking/overhead bookmarks restore the absolute target position, pitch, yaw, and zoom distance. The appropriate camera mode is selected automatically. Tracking a train when saving still creates a fixed bookmark at that location, not a moving train-relative bookmark.

Teleportation requests terrain loading before restoring the camera. Native loading progress appears only if the load takes long enough. Repeated jumps cancel the previous pending jump. World unload and mod disable cancel loading and remove the toolbar button.

## Saved data

Local file, shared between maps:

`Documents\Railroader\ModSettings\czbuchi\teleporter.json`

The DLL reads only its own profile. It does not scan, import, or depend on RailForge configuration.

Create new bookmarks in the current map; old RailForge coordinates are not imported.

Edits write immediately using atomic replacement; `.bak` holds the previous file. A failed write does not commit in-memory changes. Invalid profiles stop mod initialization and are not silently replaced with empty data. Native profile files use UTF-8 with BOM and CRLF.

## Editing the UI / future Relay input support

Both windows and their interaction flow are implemented in `Scripts/Main.rl`. Change this file to adjust the UI; restart the game to reload the pack. DLL changes always require a restart too.

The DLL patches `ScriptInterpreterFactory.CreatePackScopedBuilder` with a Harmony postfix to register a native module named `teleporter` only for this pack's full runtime scripts. It does not register it globally or expose it to Data.rl.

The `textInput(...)` helper in Main.rl is the temporary text-input adapter. It calls `tp.inputField(...)`, which unwraps the native panel builder and adds the game's standard text field. Once an official Relay input API is released, update this helper to its documented signature; no future API shape is assumed and no built-in Relay method is overridden.

Main.rl owns grouping, selection, and forms. `tp.scrollView(...)` adds a clipped native vertical scroll view; `tp.locationRow(...)` adds a destination-icon/label row. `tp.clipWindow(...)` clips the root and disables forced vertical expansion of ordinary controls. `tp.editorColumns(...)` creates a horizontal container with a 210-pixel preferred left column (170-pixel minimum) and a flexible right column; both host their own scroll views. Only these content areas expand vertically. Buttons, inputs, rows, and header/footer bars have explicit heights. `tp.editButton(...)` adds the icon-only, right-aligned main footer. `tp.button(...)` and `tp.actionBar(...)` create fixed-height form/header actions. There is no dependency on ListDetailController or its private fields.

Selection callbacks retain both group and location names so identical names in different groups remain distinct. `tp.rename(...)` moves/renames the stored view without capturing the current camera. Other bridge functions handle lists, capture for add/replace, deletion, ordering, and auto-open. The DLL uses native strategy camera state capture/restore, validated reflection for camera-mode selection and inertia reset, and no camera update transpilers.

## Validation and remaining in-game checks

The test suite executes the shipped Main.rl in the installed game's actual Relay VM with simulated UI controls and native actions. It covers groups and icon-row targets, duplicate names, repeated teleport requests, separate editor columns and scroll areas, the icon footer after content (including an empty profile), rename/move, collision rejection, confirmation, refreshes, preference placement, and world cleanup. Storage and installed API members are checked too. These tests do not measure Unity rectangles or verify rendered layout.

These tests do not run Unity. Check in game:

1. Open the toolbar window; verify group headers and destination icons, with all locations in one scroll box. Resize narrow and use long labels/many entries; no content should draw beyond the window or scroll viewport. Click icons in different groups, then the same icon again.
2. Open the editor using the main window's bottom-right icon; verify New location is a normal-height header button, and the independent left list and right form fill the available body. Resize short/narrow and scroll both areas. Selecting a location must not teleport. Add a first-person view, move and rotate, then return and compare heading and pitch.
3. Repeat in tracking view with a different zoom and while following a train. Verify the bookmark stays at the saved absolute location.
4. Confirm the first replace/delete click changes only the confirmation label; the second performs the edit.
5. Rename/move a view using Save name / group. Confirm its saved camera is unchanged, the lists refresh, and occupied target names are rejected. Verify Preferences is below both editor columns and disappears in New location mode.
6. Close/reopen both windows, reload a world, and disable/re-enable the mod. Verify no duplicate toolbar icons or stale windows.
7. Check long-distance loading, repeated clicks, and multiplayer clients. Bookmark data remains local to each player.

Diagnostic prefix: `[Teleporter]` for toolbar errors; native mod-context logging for lifecycle, profile, bridge, and camera errors.

References: https://docs.railroadergame.com/modding/dll-mods/ and https://docs.railroadergame.com/modding/relay-api/railroader/ui-windows/
