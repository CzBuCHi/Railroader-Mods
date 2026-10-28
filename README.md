# Railroader Mods - experimental

This orphan branch contains native mods for Railroader 0.6. Legacy RailForge mods and their history remain on `master` (checkpoint `575e7e2`). The empty root of this branch is `7ea4503`.

## Teleporter 3.0.5

Hybrid native DLL and Relay mod. The main window shows groups and destination-icon rows inside one clipped scroll box, with an icon-only editor button at the bottom right. The editor uses two independent scrollable columns, without the game's list/detail component. Controls have fixed heights; only the scroll areas expand. Rename/move preserves the saved view. Preferences sit below the columns and are hidden while adding. See [Teleporter guide](Teleporter/README.md).

## Tweaks 3.0.10

Selected native port: next-interchange Sleep, wagon speed/oil, metric distances and train length, camera-only fault HUD buttons, Manual/Road/Yard controls (none selected in AEWP), roster Shift-follow, a default 6-MPH manual pushing cap, and crew/repair-destination copying. JSON settings control each feature, oil threshold, and manual-push cap; no settings UI is added. See the [full included/excluded list and configuration](Tweaks/README.md).

## WaybillMap 1.3.0

Native map mod with fixed-size vehicle markers, destination colors (including locomotive repair/depot destinations), gentle spotted dimming, three vehicle filters and zoom-dependent wagon sampling. Upper-right map controls provide switch Save/Restore and Follow selected car. SwitchNormalizer is merged into this mod; its existing profile is retained and the separate mod should be disabled/removed. See the [WaybillMap guide](WaybillMap/README.md).

Build from the repository root:

```powershell
dotnet publish Teleporter/Teleporter.csproj -c Release
dotnet run --project tests/Teleporter.Tests -c Release -- Teleporter/Scripts/Main.rl "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Teleporter/bin/Release/CzBuCHi.Teleporter.dll
dotnet publish Tweaks/Tweaks.csproj -c Release
dotnet run --project tests/Tweaks.Tests -c Release -- "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Tweaks/bin/Release/CzBuCHi.Tweaks.dll
dotnet publish WaybillMap/WaybillMap.csproj -c Release
dotnet run --project tests/WaybillMap.Tests -c Release -- "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" WaybillMap/bin/Release/CzBuCHi.WaybillMap.dll
```

Default game location: `C:\Steam\steamapps\common\Railroader`. Override `GameDir` in an untracked `Paths.user` MSBuild project or pass `-p:GameDir="D:\path\Railroader"`.

All current mods target .NET Framework 4.8 with C# 9 and the installed game's framework references. Game DLLs are not distributed. Publishing only creates packages; it does not install or launch the game.

Teleporter ZIP output: `bin/CzBuCHi.Teleporter_3.0.5.zip`. The game already supplies its Harmony dependency; no separate loader or bundled Harmony copy is needed.

Tweaks ZIP output: `bin/CzBuCHi.Tweaks_3.0.10.zip`.

WaybillMap ZIP output: `bin/CzBuCHi.WaybillMap_1.3.0.zip`.
