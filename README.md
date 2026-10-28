# Railroader Mods - experimental

This orphan branch contains native mods for Railroader 0.6. Legacy RailForge mods and their history remain on `master` (checkpoint `575e7e2`). The empty root of this branch is `7ea4503`.

## Switch Normalizer 3.0.0

Native DLL mod with a toolbar icon. Click to restore switch positions; Shift-click to save them. Requires no RailForge, UMM, or Harmony.

See [installation, profile location, and checks](SwitchNormalizer/README.md).

## Teleporter 3.0.5

Hybrid native DLL and Relay mod. The main window shows groups and destination-icon rows inside one clipped scroll box, with an icon-only editor button at the bottom right. The editor uses two independent scrollable columns, without the game's list/detail component. Controls have fixed heights; only the scroll areas expand. Rename/move preserves the saved view. Preferences sit below the columns and are hidden while adding. See [Teleporter guide](Teleporter/README.md).

## Tweaks 3.0.0

Selected native port: next-interchange Sleep, wagon speed and lubrication percentage, camera-only air/handbrake/oil HUD buttons, and Manual/Road/Yard controls with no selected button in native AE Waypoint mode. The other old Tweaks features are omitted; see the [full included/excluded list](Tweaks/README.md).

Build from the repository root:

```powershell
dotnet publish SwitchNormalizer/SwitchNormalizer.csproj -c Release
dotnet run --project tests/SwitchNormalizer.Tests -c Release -- SwitchNormalizer/bin/Release/CzBuCHi.SwitchNormalizer.dll
dotnet publish Teleporter/Teleporter.csproj -c Release
dotnet run --project tests/Teleporter.Tests -c Release -- Teleporter/Scripts/Main.rl "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Teleporter/bin/Release/CzBuCHi.Teleporter.dll
dotnet publish Tweaks/Tweaks.csproj -c Release
dotnet run --project tests/Tweaks.Tests -c Release -- "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Tweaks/bin/Release/CzBuCHi.Tweaks.dll
```

Default game location: `C:\Steam\steamapps\common\Railroader`. Override `GameDir` in an untracked `Paths.user` MSBuild project or pass `-p:GameDir="D:\path\Railroader"`.

SwitchNormalizer targets .NET Framework 4.7.2; Teleporter and Tweaks target 4.8 to match the game's bundled Harmony assembly. All use C# 9 and the installed game's framework references because Unity 6 exposes newer framework types. Game DLLs are not distributed.

ZIP output: `bin/CzBuCHi.SwitchNormalizer_3.0.0.zip`. Publishing only builds the package; it does not install or launch the game.

Teleporter ZIP output: `bin/CzBuCHi.Teleporter_3.0.5.zip`. The game already supplies its Harmony dependency; no separate loader or bundled Harmony copy is needed.

Tweaks ZIP output: `bin/CzBuCHi.Tweaks_3.0.0.zip`.
