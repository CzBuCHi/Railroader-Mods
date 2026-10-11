# TurntableControl

Mod ID: CzBuCHi.TurntableControl. Current version: 2.0.4.

Click a track around a turntable to rotate the bridge toward it at normal motor speed.

## Controls and arrows

Hover a connected track's clickable area to show a gold arrow pointing outward from the turntable center. A second arrow appears on the bridge's hut end. Both disappear when the pointer leaves the area.

The bridge arrow marks the end that will align with the selected track. When aligned, the two arrows share an axis with a visible gap; their outlines do not overlap.

- Click a track arrow to choose the target.
- Click another target while rotating to change the destination.
- Shift-click an arrow to stop the turntable.
- Move the native lever manually to cancel automatic movement and retain manual control.

## Rotation

The mod chooses the shortest rotation direction and sets the native lever to -100% or +100%. The game's TurntableController handles acceleration, bridge movement, and vehicle movement. Its native maximum speed is 10 degrees per second.

At the destination the lever returns to neutral and the bridge connects to the track. Final angle correction is limited to the last normal physics step. Reversing direction retains the game's inertia.

Native checks for vehicles extending beyond the bridge and rotation obstacles remain active. If they stop the motor, automatic movement is cancelled.

## Multiplayer and saves

Only the host drives rotation. Clients send the selected index through the turntable's registered KeyValueObject, using the lever's permissions. Install the mod on the host and any client that wants to use the clickable arrows.

The temporary target uses CzBuCHi.TurntableControl.targetIndex. Loading a map clears an old automatic request and returns its lever to neutral; a saved automatic trip does not resume by itself.

## Build and checks

    dotnet publish TurntableControl/TurntableControl.csproj -c Release

Rotation tests cover shortest paths, wraparound, inertia, and 2,508 combinations of starting position, target, speed, and physics step. Unity collision handling and multiplayer require in-game checks.

Diagnostic log prefix: [TurntableControl].
