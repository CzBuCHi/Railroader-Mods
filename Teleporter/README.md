# Teleporter 3.0.0 - Relay UI + native DLL

For Railroader 0.6.x. Identifier: `czbuchi/teleporter`.

## Install

1. Close the game and disable the old Teleporter so only one version runs.
2. Extract the ZIP's `CzBuCHi.Teleporter` folder into `Documents\Railroader\Mods`.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods**, restart, then enable **Teleporter** in Mods and click Apply.

Keep the DLL and `Scripts/Main.rl` together. RailForge and UMM are not required. The mod uses the `0Harmony.dll` already supplied in the game's Managed directory; the ZIP does not distribute game libraries.

## Main window

Open Teleporter using its portal icon in the top-right toolbar, or `/teleporter` in the game console.

Choose a group. Each saved place uses the same native location-field prefab as wagon locations: a destination icon followed by a label. Click the icon to restore the saved view. Adding, replacing, deleting, and reordering are kept out of this window.

**Edit locations...** opens a separate editor. Both windows can be moved and resized.

## Editor

- Enter a group name and location name, then **Add current view**. A new group is created if needed; an existing name is never silently overwritten.
- Choose a saved group and location to replace its view or delete it. Both actions require a second click to confirm.
- Move groups up/down using the editor buttons. Locations are sorted by name.
- Set **Open Teleporter on world load** here.

The main window and editor refresh after successful changes. Text fields update their draft values while typing. The editor stores nothing until an action is confirmed.

## Camera bookmarks

First-person bookmarks restore player position and viewing direction. Tracking/overhead bookmarks restore the absolute target position, pitch, yaw, and zoom distance. The appropriate camera mode is selected automatically. Tracking a train when saving still creates a fixed bookmark at that location, not a moving train-relative bookmark.

Teleportation requests terrain loading before restoring the camera. Native loading progress appears only if the load takes long enough. Repeated jumps cancel the previous pending jump. World unload and mod disable cancel loading and remove the toolbar button.

## Saved data and migration

Local file, shared between maps:

`Documents\Railroader\ModSettings\czbuchi\teleporter.json`

The `Groups`, `Name`, `Locations`, `Position`, `Rotation`, `Distance`, `FirstPerson`, and `AutoOpenTeleporterWindow` fields remain compatible with the old Teleporter format. When the new file does not exist, the mod imports the old RailForge profile from the game directory:

`Mods\RTM.RailForge\LegacySettings\CzBuCHi.Teleporter--CzBuCHi.Teleporter.json`

The old file is never changed. You can also manually copy an old profile to the new location with the game closed. Absolute coordinates depend on the map.

Edits write immediately using atomic replacement; `.bak` holds the previous file. A failed write does not commit in-memory changes. Invalid profiles stop mod initialization and are not silently replaced with empty data. Native profile files use UTF-8 with BOM and CRLF.

## Editing the UI / future Relay input support

Both windows and their interaction flow are implemented in `Scripts/Main.rl`. Change this file to adjust the UI; restart the game to reload the pack. DLL changes always require a restart too.

The DLL patches `ScriptInterpreterFactory.CreatePackScopedBuilder` with a Harmony postfix to register a native module named `teleporter` only for this pack's full runtime scripts. It does not register it globally or expose it to Data.rl.

The `textInput(...)` helper in Main.rl is the temporary text-input adapter. It calls `tp.inputField(...)`, which unwraps the native panel builder and adds the game's standard text field. Once an official Relay input API is released, update this helper to its documented signature; no future API shape is assumed and no built-in Relay method is overridden.

`tp.locationRow(...)` adds the native wagon-style icon/label row with a bookmark callback. Other bridge functions handle group/name lists, camera capture for add/replace, deletion, group ordering, and the auto-open preference. The DLL uses native strategy camera state capture/restore, validated reflection for camera-mode selection and inertia reset, and no camera update transpilers.

## Validation and remaining in-game checks

The test suite executes the shipped Main.rl in the installed game's actual Relay VM with simulated UI controls and native actions. It covers adding through both text fields, the main location rows, separate editor actions, two-step replacement/deletion, error handling, refreshes, preferences, and world cleanup. It also tests profile persistence, migration, corruption, backups, failed writes, and the installed game API members used by the bridge.

These tests do not run Unity. Check in game:

1. Open the toolbar window; verify icon/label rows look like wagon locations and only the destination icon teleports.
2. Open the editor, type a group/name, and add a first-person view. Move and rotate, then return and compare heading and pitch.
3. Repeat in tracking view with a different zoom and while following a train. Verify the bookmark stays at the saved absolute location.
4. Confirm the first replace/delete click changes only the confirmation label; the second performs the edit.
5. Close/reopen both windows, reload a world, and disable/re-enable the mod. Verify no duplicate toolbar icons or stale windows.
6. Check long-distance loading, repeated clicks, and multiplayer clients. Bookmark data remains local to each player.

Diagnostic prefix: `[Teleporter]` for toolbar errors; native mod-context logging for lifecycle, profile, bridge, and camera errors.

References: https://docs.railroadergame.com/modding/dll-mods/ and https://docs.railroadergame.com/modding/relay-api/railroader/ui-windows/
