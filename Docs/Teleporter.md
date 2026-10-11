# Teleporter

Mod ID: CzBuCHi.Teleporter. Current version: 2.0.5.

Save a camera or character location and return to it with one click. Locations are organized into named groups.

## Window controls

Open the window with its portal icon in the top toolbar. The top section contains Save current and the name fields; groups appear on the left and their locations on the right.

1. Select a group or enter a new Group name.
2. Enter a unique New location name.
3. Click Save current.
4. Click a location's destination icon to return to it.

The copy icon replaces a location with the current view. The multiplication sign (×) deletes it. Replacement and deletion require a second confirmation click. Arrow buttons reorder groups.

The native-style window can be moved and resized. Scrolling and clipping keep its contents inside the window. The AutoOpenTeleporterWindow preference can open it automatically after loading a map.

## What is saved

First-person entries store the character's world position and viewing direction. Tracking-camera entries store position, orientation, and zoom. Returning to an entry restores its saved viewing mode.

Older entries without a FirstPerson value retain tracking-camera behavior. Coordinates are absolute: saving while standing on a car does not make the saved location move with that car.

## Loading indicator and data

The destination area loads before the view moves. Native Loading appears only when loading takes long enough; a short move into an already loaded area may show no indicator. Cancellation, a new destination, or leaving the map clears the indicator.

Changes are saved immediately through RailForge under CzBuCHi.Teleporter. Existing compatible groups, ordering, and locations are retained. The location list is shared between maps, matching the original mod, and empty groups remain available.

## Build and checks

    dotnet publish Teleporter/Teleporter.csproj -c Release

Requires RTM.RailForge. The mod does not use Railloader API or Harmony patches. Data checks cover legacy groups, ordering, coordinates, viewing modes, and JSON round trips. Check first-person restoration, tracking zoom, remote destinations, replacement, deletion, and map reloads in-game.
