# Teleporter 3.0.2 - Relay UI + native DLL

For Railroader 0.6.x. Identifier: `czbuchi/teleporter`.

## Install

1. Close the game and disable the old Teleporter so only one version runs.
2. Extract the ZIP's `CzBuCHi.Teleporter` folder into `Documents\Railroader\Mods`.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods**, restart, then enable **Teleporter** in Mods and click Apply.

Keep the DLL and `Scripts/Main.rl` together. RailForge and UMM are not required. The mod uses the `0Harmony.dll` already supplied in the game's Managed directory; the ZIP does not distribute game libraries.

## Main window

Open Teleporter using its portal icon in the top-right toolbar, or `/teleporter` in the game console.

All saved locations appear together in one native list, styled like the wagon purchase list: group headings followed by their locations. No group dropdown or right-hand detail column is present. Click a location row to restore its saved view, including repeated clicks on the same row.

Groups follow the saved group order; locations are sorted by name within each group. Empty groups are omitted from the list. Adding, replacing, deleting, and reordering are kept out of this window.

**Edit locations...** opens a separate editor. Both windows can be moved and resized.

## Editor

- The left side shows the same grouped location list as the main window. Selecting a row opens its editing controls on the right; it does not teleport the camera.
- Click **New location** to display the add form on the right. Enter group/name, then **Add current view**. A new group is created if needed; an existing name is never silently overwritten.
- Select a saved location to replace its view or delete it. Both actions require a second click to confirm; selecting another location cancels pending confirmation.
- Move the selected location's group up/down using the controls on the right.
- Set **Open Teleporter on world load** in the right-hand panel.

The main window and editor refresh after successful changes. Text fields update their draft values while typing. The editor stores nothing until an action is confirmed.

## Camera bookmarks

First-person bookmarks restore player position and viewing direction. Tracking/overhead bookmarks restore the absolute target position, pitch, yaw, and zoom distance. The appropriate camera mode is selected automatically. Tracking a train when saving still creates a fixed bookmark at that location, not a moving train-relative bookmark.

Teleportation requests terrain loading before restoring the camera. Native loading progress appears only if the load takes long enough. Repeated jumps cancel the previous pending jump. World unload and mod disable cancel loading and remove the toolbar button.

## Saved data and migration

Local file, shared between maps:

`Documents\Railroader\ModSettings\czbuchi\teleporter.json`

The DLL reads only its own profile. It does not scan, import, or depend on RailForge configuration.

For a one-time migration, use the standalone `Migrate-RailForgeProfile.ps1` included beside the DLL. Requires **PowerShell 7.3 or later** (`pwsh`). Close Railroader first. Open PowerShell in the extracted mod folder and run:

```powershell
pwsh -NoProfile -File .\Migrate-RailForgeProfile.ps1
```

Default source: `C:\Steam\steamapps\common\Railroader\Mods\RTM.RailForge\LegacySettings\CzBuCHi.Teleporter--CzBuCHi.Teleporter.json`. Default destination: the local profile path shown above, using your Windows Documents folder.

Preview and validate without writing:

```powershell
pwsh -NoProfile -File .\Migrate-RailForgeProfile.ps1 -WhatIf
```

If the game is installed elsewhere:

```powershell
pwsh -NoProfile -File .\Migrate-RailForgeProfile.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Railroader'
```

For a backed-up RF file or a custom destination:

```powershell
pwsh -NoProfile -File .\Migrate-RailForgeProfile.ps1 -SourcePath 'D:\backup\Teleporter.json' -DestinationPath 'D:\profiles\teleporter.json'
```

If a new-mod profile already exists, the script stops. Add `-Force` only when you intend to replace it; this creates a uniquely named `teleporter.json.migration-<id>.bak` beside it. The script replaces the profile, rather than merging locations. The RF source is never changed. On success it prints the source, destination, group/location counts, and backup path if applicable.

The converter preserves group order, names (including case-sensitive names), positions, rotations, zoom, camera mode, and the auto-open preference. Missing legacy `FirstPerson` defaults to tracking view; a missing auto-open preference defaults to false. Invalid data is rejected before writing. Restart/load the game after migration. Absolute coordinates still depend on the map.

Edits write immediately using atomic replacement; `.bak` holds the previous file. A failed write does not commit in-memory changes. Invalid profiles stop mod initialization and are not silently replaced with empty data. Native profile files use UTF-8 with BOM and CRLF.

## Editing the UI / future Relay input support

Both windows and their interaction flow are implemented in `Scripts/Main.rl`. Change this file to adjust the UI; restart the game to reload the pack. DLL changes always require a restart too.

The DLL patches `ScriptInterpreterFactory.CreatePackScopedBuilder` with a Harmony postfix to register a native module named `teleporter` only for this pack's full runtime scripts. It does not register it globally or expose it to Data.rl.

The `textInput(...)` helper in Main.rl is the temporary text-input adapter. It calls `tp.inputField(...)`, which unwraps the native panel builder and adds the game's standard text field. Once an official Relay input API is released, update this helper to its documented signature; no future API shape is assumed and no built-in Relay method is overridden.

`tp.locationBrowser(...)` adds the game's native grouped list/detail control, hides its detail column, expands the list to fill the window, and connects row clicks to teleportation. `tp.locationEditor(...)` uses the same control with a 220-pixel list on the left and a Relay callback that builds the editing panel on the right. Selection identifiers encode both group and location names so identical location names in different groups remain distinct. Other bridge functions handle group/name lists, camera capture for add/replace, deletion, group ordering, and the auto-open preference. The DLL uses native strategy camera state capture/restore, validated reflection for camera-mode selection and inertia reset, and no camera update transpilers.

## Validation and remaining in-game checks

The test suite executes the shipped Main.rl in the installed game's actual Relay VM with simulated UI controls and native actions. It covers all groups in one list, group order, duplicate location names across groups, repeated one-click teleport requests, editor list selection and right-hand forms, two-step replacement/deletion, cancellation on selection change, error handling, refreshes, preferences, and world cleanup. The shared catalog, profile persistence, backups, failed writes, and installed API members used by the bridge are also checked. A separate PowerShell test covers the standalone migration script.

These tests do not run Unity. Check in game:

1. Open the toolbar window; verify every populated group appears in one purchase-style list, with no right column. Click rows in different groups without using a dropdown, then click the same row again.
2. Open the editor; verify the same list is on the left and selecting a location displays controls on the right without teleporting. Click New location, type a group/name, and add a first-person view. Move and rotate, then return and compare heading and pitch.
3. Repeat in tracking view with a different zoom and while following a train. Verify the bookmark stays at the saved absolute location.
4. Confirm the first replace/delete click changes only the confirmation label; the second performs the edit.
5. Close/reopen both windows, reload a world, and disable/re-enable the mod. Verify no duplicate toolbar icons or stale windows.
6. Check long-distance loading, repeated clicks, and multiplayer clients. Bookmark data remains local to each player.

Diagnostic prefix: `[Teleporter]` for toolbar errors; native mod-context logging for lifecycle, profile, bridge, and camera errors.

References: https://docs.railroadergame.com/modding/dll-mods/ and https://docs.railroadergame.com/modding/relay-api/railroader/ui-windows/
