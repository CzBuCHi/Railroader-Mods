# Railroader Mods - experimental

This orphan branch contains native mods for Railroader 0.6. Legacy RailForge mods and their history remain on `master` (checkpoint `575e7e2`). The empty root of this branch is `7ea4503`.

## Tweaks 3.0.14

Includes **Points of interest** (merged Teleporter): Alt+T opens saved camera views; configure the last entry in vanilla Bindings > Camera. The toolbar portal icon is removed; existing bookmarks remain in use. Disable/remove standalone Teleporter. See the [Points of interest guide](Tweaks/PointsOfInterest.md).

Selected native port: next-interchange Sleep, wagon speed/oil, metric distances and train length, camera-only fault HUD buttons, Manual/Road/Yard controls (none selected in AEWP), roster Shift-follow, a default 6-MPH manual pushing cap, and crew/repair-destination copying. JSON settings control each feature, oil threshold, and manual-push cap; no settings UI is added. See the [full included/excluded list and configuration](Tweaks/README.md).

## WaybillMap 1.5.2

Native map mod with native MapEnhancer-style vehicle markers and combined steam/tender icons, destination colors (including locomotive repair/depot destinations), gentle spotted dimming, three vehicle filters and zoom-dependent wagon sampling. Upper-right map controls provide switch Save/Restore and Follow selected car. SwitchNormalizer is merged into this mod; its existing profile is retained and the separate mod should be disabled/removed. See the [WaybillMap guide](WaybillMap/README.md).

Build from the repository root:

```powershell
dotnet publish Tweaks/Tweaks.csproj -c Release
dotnet run --project tests/PointsOfInterest.Tests -c Release -- Tweaks/Scripts/Main.rl "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Tweaks/bin/Release/CzBuCHi.Tweaks.dll
dotnet run --project tests/Tweaks.Tests -c Release -- "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Tweaks/bin/Release/CzBuCHi.Tweaks.dll
dotnet publish WaybillMap/WaybillMap.csproj -c Release
dotnet run --project tests/WaybillMap.Tests -c Release -- "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" WaybillMap/bin/Release/CzBuCHi.WaybillMap.dll
```

Default game location: `C:\Steam\steamapps\common\Railroader`. Override `GameDir` in an untracked `Paths.user` MSBuild project or pass `-p:GameDir="D:\path\Railroader"`.

All current mods target .NET Framework 4.8 with C# 9 and the installed game's framework references. Game DLLs are not distributed. Publishing only creates packages; it does not install or launch the game.

Tweaks ZIP output: `bin/CzBuCHi.Tweaks_3.0.14.zip`.

WaybillMap ZIP output: `bin/CzBuCHi.WaybillMap_1.5.2.zip`.
