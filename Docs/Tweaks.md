# Tweaks

Mod ID: CzBuCHi.Tweaks. Current version: 2.0.22.

Selected features from CarInspectorTweaks, plus consist controls and repair-shop customization. Each feature has its own RailForge settings toggle. Defaults are enabled unless noted below.

## Time and car inspection

- Sleep until next interchange: the Time window uses the earliest active interchange service, including an ordered extra service. Overdue service advances one game second; missing service information falls back to vanilla timing.
- Show car speed: whole MPH for cars; locomotives retain their native speed display.
- Show car oil: oil percentage on vehicles supporting lubrication.
- Follow and select: Shift-click the eye in the engine roster to follow and select that locomotive.
- Fast strong man: raises the manual pushing speed limit from 3 to 6 MPH while retaining native force and weight calculations.

## Copy and brake actions

- Copy repair destination: an icon beside the repair destination in Equipment copies that destination to other coupled cars.
- Copy crew: a compact icon beside the crew selector copies the assignment to suitable coupled locomotives, passenger cars, and cabooses. None clears their assignment. The button appears only when a suitable vehicle has a different assignment.
- Bleed all: All appears behind the native Bleed button and operates compatible bleed valves throughout the coupled consist.

Actions use the game's methods, state messages, and permission checks.

## Locomotive HUD

Four icon buttons replace the visible Control Mode dropdown: Manual, AE Road, AE Yard, and AE Waypoint. The original dropdown remains hidden as the backend, and the buttons call its native change handler.

Shift+G works from every mode: it selects AE Waypoint and opens the native waypoint picker.

Consist length is shown in whole meters by converting the native feet display. For example, 1931 ft becomes 589 m.

Conditional action icons appear higher in the header, beside the speed display:

| Icon | Action |
| --- | --- |
| Air hose and valve | Fix air hoses and angle cocks throughout the mechanically coupled consist. |
| White handbrake | Release applied handbrakes throughout the consist. |
| Orange oil warning | Follow the car with the lowest oil level below the configured threshold. |
| Red oil warning | Follow a car with a hotbox, even if its oil level is 100%. |

The default oil warning threshold is 50%. The warning does not refill oil. The red state replaces orange whenever the consist contains a hotbox.

## Whistles and bells

Sound customization requires native customization permission and an actual repair track.

Choose / test whistles opens a separate scrollable list. Its checkboxes allow one selected whistle. Selecting another clears the previous selection and plays a one-second preview, including its short release. Previewing does not change the locomotive's saved whistle.

- OK confirms the choice and closes the window.
- Cancel or the close button leaves the whistle unchanged.
- Previewing is available away from a repair track; confirmation requires a repair track.
- The bell selector uses RailForge's bell catalog and playback support.
- The repair-track warning appears only for a player permitted to customize the locomotive.

Manual sound installation takes 5 game minutes is enabled by default. While enabled, a manual change creates a temporary waybill and displays an installation countdown. Work order reads Sound installation. Movement pauses work; leaving that repair track cancels it. The original waybill is restored unless another order has replaced it. Pending work is saved with the locomotive.

Disable that setting for immediate manual whistle and bell changes on a repair track, without an installation waybill. A pending manual job still on its repair track completes on the next processing pass after the setting is disabled.

A manual confirmation cancels a pending prank and prevents another prank during that repair-track visit, including when confirming the current whistle.

## Repair shop prankster

The host checks player-owned locomotives stopped on actual repair tracks. Stopped means below 0.1 MPH.

The prankster always waits five game minutes, independently of the manual installation setting. Its work has no waybill or UI countdown and does not block manual customization. Movement pauses its timer; leaving the repair track cancels unfinished work.

When complete, it chooses different whistle and bell sounds where alternatives exist. A saved repair-track flag prevents repeated changes during the same visit or after reloading. Another repair track permits another prank. Stopping outside a repair track clears the flag; merely passing outside does not.

Prankster changes lettering also changes an eligible sign at completion. A tender-equipped steam locomotive uses its adjacent tender. Tank locomotives and diesels use their own lettering. Models without native lettering support are skipped. Vehicle numbers and other cars' signs are not changed.

## PrankTexts.txt

The file sits beside the mod DLL. It contains 50 English, non-vulgar default phrases. Preserve your customized copy when updating.

- One physical line is one choice.
- Type the literal sequence \n for a line break inside a sign.
- Blank lines are ignored; comments have no special meaning.
- Choices are trimmed, deduplicated, and limited to 100 characters.
- A missing or empty list skips lettering changes.

Edit prank texts appears in settings only while Prankster changes lettering is enabled. In the editor, Enter creates another choice and \n remains literal. Save writes UTF-8 with BOM and CRLF and creates PrankTexts.txt.bak. The list is read at each prank, so edits do not require a restart.

## Build and checks

    dotnet publish Tweaks/Tweaks.csproj -c Release

Requires RTM.RailForge and the installed game assemblies. The host must have Fast strong man installed to apply its physics change.

The test suite checks interchange timing, the installed game's push IL, enabled and disabled push limits, repair-track rearming, prank text parsing, and five-minute timer behavior. Window layout, previews, permission handling, and multiplayer still require in-game checks.

Diagnostic log prefix: [Tweaks].
