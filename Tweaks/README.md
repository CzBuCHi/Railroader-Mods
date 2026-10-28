# Tweaks 3.0.0 - selected Railroader 0.6 features

Native DLL mod. Identifier: `czbuchi/tweaks`. Requires Railroader 0.6.x and its bundled Harmony; no RailForge or UMM is required.

## Install

1. Close the game and disable/remove the old Tweaks to avoid duplicate patches.
2. Extract the ZIP's `CzBuCHi.Tweaks` directory into `Documents\Railroader\Mods`.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods**, restart, enable **Tweaks** in Mods, and click Apply.

The mod is optional in multiplayer. It adds local UI and changes the native Sleep calculation. Native controls still handle mode changes, authorization, and waiting. Camera buttons do not send car-state changes. UI layout, multiplayer, and camera behavior still need in-game checks.

## Included

### Sleep until the next interchange

The Time window's existing Sleep button and interval label use the earliest next service among enabled interchanges, including a scheduled extra/custom service. They query the game's current service times each time; nothing is scheduled by this mod. An overdue service advances one game second. If no valid service time is available, the native timing calculation is used. The tooltip explains the updated behavior. Native waiting permissions remain in effect.

### Car inspector speed and oil

The Car tab shows whole-MPH speed for non-locomotive vehicles. Locomotives retain their native speed display. Vehicles with lubrication enabled show their Oiled percentage. The game's oil feature and vehicle support determine visibility, so unsupported vehicles such as diesels do not gain an oil field. Values update while the panel is open. Reopen the panel after changing the global oil feature.

### Locomotive HUD: camera navigation only

Conditional icons sit beside the selected locomotive's speed display and inspect its mechanically coupled consist. The current consist and faults are resolved again on each click.

| Icon | Visible when | Click action |
| --- | --- | --- |
| Air hose / valve | An internal hose is disconnected, an internal anglecock is not fully open, or an outer anglecock is open | Center the tracking camera on the first affected logical coupler end. |
| White handbrake wheel | A car has an applied handbrake | Show a car with an applied handbrake. |
| Orange oil warning | A lubrication-enabled car has less than 50% oil | Show the eligible car with the lowest oil. |
| Red oil warning | An eligible car has a hotbox | Show a hotbox car, ahead of ordinary low-oil warnings, including a hotbox on a car refilled to 100%. |

These are navigation tools. They do not connect hoses, turn anglecocks, release brakes, refill oil, or clear hotboxes. Repair the problem manually; the icon scan updates about twice per second and a subsequent click finds the next remaining problem. Air uses the affected car end's track position, so it can navigate even when that car's detailed model is not loaded yet. Current camera zoom is retained.

### Manual / AE Road / AE Yard

Three icon buttons replace the visible native Control Mode dropdown: Manual, AE Road, and AE Yard. They call the native mode-change handler and mirror the native dropdown's enabled state, including MU restrictions. The hidden dropdown remains the backend.

AE Waypoint has no mod button. When the game selects waypoint mode after setting a waypoint, all three mod buttons remain unselected. No waypoint shortcut or automatic mode-selection patch is installed.

If HUD button creation fails, the mode component restores the native dropdown and logs the error. HUD objects and owned icon textures are cleaned up on world unload/mod disable. Reopen native inspector/time windows after enabling/disabling the mod to refresh their existing content.

The selected features are always enabled; this reduced port has no legacy settings page, settings import, or new configuration file. The oil warning threshold retains the old default of 50%.

## Deliberately not included

The following old Tweaks additions were not ported. Native game functionality remains available:

- Rotating autosave slots, slot-count settings, renumbering/rollback, and the pause-menu latest-slot display.
- Compact timetable departure text in the engine roster.
- Shift-click eye to follow and select a roster locomotive.
- Fast strong man / the 6-MPH manual pushing limit.
- Copy repair destination to coupled cars.
- Copy crew assignments to coupled vehicles.
- Bleed all coupled cars from the inspector.
- Consist length conversion from feet to meters in the HUD/tooltip.
- A dedicated AE Waypoint mode button.
- Shift+G waypoint selection from every mode.
- Custom whistle picker, sound previews, bell selection, and repair-track customization controls.
- Five-minute manual sound installation, temporary work-order waybills/countdown, and installation settings.
- Repair-shop sound prankster and its repair-visit timing/persistence.
- Prank lettering, PrankTexts.txt, and its editor.
- Old RailForge settings UI and configuration import.

The old automatic fix-air and release-all-handbrakes actions are intentionally replaced by camera navigation, per the requested new behavior.

## Build and validation

```powershell
dotnet publish Tweaks/Tweaks.csproj -c Release
dotnet run --project tests/Tweaks.Tests -c Release -- 'C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed' Tweaks/bin/Release/CzBuCHi.Tweaks.dll
```

Set GameDir via Paths.user or `-p:GameDir=...` when needed. Target: .NET Framework 4.8 / C# 9 using installed Unity/game framework assemblies. Publishing creates `bin/CzBuCHi.Tweaks_3.0.0.zip`; it does not deploy or launch the game. No game or Harmony DLLs are distributed.

Tests cover daily/extra service selection, overdue/midnight/invalid service times, three-button highlights including no selection in waypoint mode, air fault conditions, oil/hotbox priority and feature gating, installed patch targets, the installed Sleep tooltip literal, compiled native lifecycle interfaces, omitted legacy features, and the absence of brake/air state-writing calls. They do not run Unity.

In-game checks:

1. Schedule extra interchange service earlier than regular service. Confirm Sleep's label and actual advance use the extra service; disable that interchange and verify it is ignored.
2. Inspect a moving wagon and a lubricated vehicle. Check live speed/oil; turn lubrication off and reopen the panel to confirm the oil field disappears.
3. Create an air problem and an applied handbrake in the selected consist. Click the icons and verify camera targets; confirm the faults remain unchanged until manually repaired.
4. Check low oil and a hotbox: the red warning must take priority without changing either state.
5. Select Manual, Road, Yard, then set a waypoint using the native game controls. AEWP must leave every mod mode button unselected. Check disabled buttons with MU enabled.
6. Change selected locomotives, reload a world, and disable/re-enable the mod. Check for duplicate icons and restored native controls. Verify placement at the UI scales you use.

Diagnostic prefix: `[Tweaks]`, plus the native mod-context lifecycle log.
