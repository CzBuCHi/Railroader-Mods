# Railroader mods for RailForge

RailForge ports of SwitchNormalizer, TurntableControl, Teleporter, and selected CarInspectorTweaks features. The experimental code in the old repository's src folder is not used.

| Mod | Version | Purpose |
| --- | --- | --- |
| [SwitchNormalizer](Docs/SwitchNormalizer.md) | 2.0.4 | Save and restore switch positions from the toolbar. |
| [TurntableControl](Docs/TurntableControl.md) | 2.0.4 | Rotate a turntable toward a selected track using its normal motor. |
| [Teleporter](Docs/Teleporter.md) | 2.0.5 | Save grouped first-person and tracking-camera locations. |
| [Tweaks](Docs/Tweaks.md) | 2.0.22 | Car inspection, consist actions, control modes, and repair-shop sound customization. |

See the [documentation index](Docs/README.md) and [build guide](Docs/Build.md).

## Installation

Install RTM.RailForge first. Close Railroader, then extract the package's Mods directory into the game directory. Keep only one copy of each mod and remove its old Railloader version from Mods.

When updating Tweaks, preserve your customized PrankTexts.txt.

## Build

Requires the .NET SDK, the installed game assemblies, and RailForge. The test projects use .NET 10.

Copy Paths.user.sample to Paths.user and set GameDir. Paths.user is local configuration and is ignored by Git. Command-line -GameDir overrides it.

    dotnet publish -c Release
    dotnet publish Tweaks/Tweaks.csproj -c Release
    dotnet publish -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Railroader"

Run from the repository root. Railroader-Mods.slnx selects all four mods. Publishing runs the applicable checks and creates installable ZIPs in bin. It does not install mods or publish GitHub releases. Plain dotnet publish also works; Release is recommended for distribution.

Source text files use UTF-8 with BOM and CRLF line endings.
