# TurntableControl 2.0.4

Requires RTM.RailForge. Mod ID: CzBuCHi.TurntableControl.

Hover a connected track around a turntable to show its target arrow and the bridge's hut-end arrow. Click to rotate toward the target using the normal motor at full lever power. Shift-click to stop. A manual lever change cancels automatic movement.

The mod uses the shortest direction, preserves acceleration and inertia, and respects native obstacle checks. Only the host drives rotation; clients send permitted target requests. Old automatic requests are cleared when a map loads.

Close the game and extract Mods/CzBuCHi.TurntableControl into its game directory. Keep only one installed version.

Build from the repository root with dotnet publish TurntableControl/TurntableControl.csproj -c Release. Rotation checks run automatically. The ZIP is created in the repository bin directory. The release package includes the detailed guide at docs/TurntableControl.md.
