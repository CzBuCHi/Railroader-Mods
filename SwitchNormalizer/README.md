# Switch Normalizer 3.0.0

Native DLL mod for Railroader 0.6.x. Identifier: `czbuchi/switch-normalizer`.

## Install

1. Close Railroader. Disable/remove the old SwitchNormalizer from your legacy loader so only one version runs.
2. Extract the ZIP's `CzBuCHi.SwitchNormalizer` folder into `Documents\Railroader\Mods`.
3. In Preferences > Features > Experimental, enable **Allow Unsafe DLL Mods**, then restart the game.
4. Enable **Switch Normalizer** in the main menu's Mods screen and click Apply.

RailForge, UMM, and Harmony are not required. Native DLL loading is experimental. The mod uses internal game track/UI classes, so later game updates may require another build.

## Controls

- Click the switch icon in the top-right toolbar to restore saved positions.
- Hold either Shift key while clicking to save the current positions.
- The tooltip distinguishes both actions. There is no keyboard shortcut.
- The button appears after a world loads and is removed on world unload or mod disable.

The profile stores only IDs of switches in the reversed (`isThrown = true`) position. All other eligible switches return to `false`, including switches added after saving. Without a profile, restoration sets eligible switches to `false`. This means the track's normal state, which does not always correspond to the visually straight route.

Restoration skips locked CTC switches and switches blocked by rolling stock. Requests pass through native `RequestSetSwitch` messages, retaining host authority and player permissions. A client log reports requested changes, not confirmation that the host accepted each request. The mod is optional in multiplayer; clients/host without it can still join. Multiplayer operation needs in-game verification.

## Profile

The local profile is shared between maps, matching the old mod:

`Documents\Railroader\ModSettings\czbuchi\switch-normalizer.json`

```json
{
  "ThrownSwitches": ["switch-id-1", "switch-id-2"]
}
```

Saving writes immediately using an atomic replacement; `.bak` retains the previous profile. Invalid or unreadable profiles abort restoration and produce a log error. No profile data is written during restoration.

To migrate the old profile, copy its JSON to this location with the game closed. The old RailForge file is `Mods\RTM.RailForge\LegacySettings\CzBuCHi.SwitchNormalizer--CzBuCHi.SwitchNormalizer.json` under the game directory. The JSON must contain a `ThrownSwitches` array of nonempty string IDs. Automatic import is not performed. Old IDs only apply if the map/update preserves them.

## Validation

Build and profile/assembly checks run locally against the installed game assemblies. Those checks cover empty/default profiles, deduplication, persistence, backup, invalid data, failed atomic replacement, encoding, embedded icon, and native entry-point interfaces. They do not run Unity.

In-game checks still required:

1. Load a world: confirm one toolbar icon and the save/restore tooltip.
2. Shift-click with a mix of normal/reversed switches, change them, then click to restore.
3. Confirm occupied and locked CTC switches remain protected.
4. Reload the world/game and verify saved positions survive, with no duplicate icons.
5. Disable/re-enable the mod; check cleanup and repeat on a multiplayer client with appropriate permissions.

Diagnostic prefix: `[SwitchNormalizer]` for toolbar failures; native mod context logging for lifecycle, save/restore results, and profile failures.

## References

- https://docs.railroadergame.com/modding/dll-mods/
- https://docs.railroadergame.com/modding/info-json/
- https://docs.railroadergame.com/modding/installing-mods/
