# SwitchNormalizer

Mod ID: CzBuCHi.SwitchNormalizer. Current version: 2.0.4.

Save a preferred switch layout and restore it with one toolbar click.

## Controls

- Click the switch icon in the top toolbar to restore the saved layout.
- Shift-click the icon to save the current layout immediately.
- Without a saved profile, restoration sets all eligible switches to isThrown = false.
- Switches added after a profile was saved also default to isThrown = false.

Restoration uses the game's RequestSetSwitch and CanSetSwitch checks. Locked CTC switches and switches the player cannot change are respected. A switch's normal orientation depends on its track layout; false does not universally mean the visually straight route.

## Saved profile

The ThrownSwitches setting stores switch IDs. The profile is shared between maps, matching the original mod.

RailForge legacy settings under the same mod ID remain compatible. The relevant file is Mods/RTM.RailForge/LegacySettings/CzBuCHi.SwitchNormalizer--CzBuCHi.SwitchNormalizer.json. The mod does not automatically import a separate standalone Railloader settings file.

## Requirements and multiplayer

Requires RTM.RailForge. Switch requests use the game's state messages and permissions, including client requests to the host.

The mod adds a toolbar icon and tooltip; it has no separate window. The icon is a 128x128 transparent RGBA PNG, scaled by the game. Toolbar tint controls its color.

## Build and checks

From the repository root:

    dotnet publish SwitchNormalizer/SwitchNormalizer.csproj -c Release

The toolbar button is removed when leaving a map or disabling the mod. To check in-game behavior, save several positions, change them, restore them, then reload the map and confirm the profile survives without duplicate icons.

Diagnostic log prefix: [SwitchNormalizer].
