# WaybillMap 1.3.0

Native Railroader 0.6.x map mod. Identifier: `czbuchi/waybill-map`. Includes the former SwitchNormalizer functionality. No RailForge/UMM is needed.

## Install

1. Close Railroader and disable/remove the separate SwitchNormalizer mod.
2. Extract `CzBuCHi.WaybillMap` from the ZIP into `Documents\Railroader\Mods`, replacing the previous WaybillMap version.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods** and restart if needed. Enable WaybillMap in Manage Mods.
4. Open the map. Map > Display > **Waybill cars** toggles the vehicle layer; the upper-right control panel provides switch tools, following and a vehicle filter.

## Vehicle layer

All logical cars are available, including parked/distant vehicles without loaded detailed models. Icons retain a fixed size in UI points when the map zoom changes. Lengths remain proportional between vehicle types (2.4 points per meter of vehicle length); wagon and locomotive width is the same 14.4 points, 20% above the previous 12-point baseline. Locomotives have a pointed nose and are rendered in the last layer, above all wagon bodies and labels. Their oversized vanilla graphics are temporarily hidden and restored when the layer is hidden or the mod unloads; original native click targets remain.

Wagons use reporting mark and road number on two lines; tenders and locomotives use the road number. Text follows vehicle orientation and flips for readability. Black/white text is chosen for contrast. Shapes, labels and positions are clipped to the map image. Marker meshes are batched within Unity UI's vertex limit and labels are pooled.

Destination colors use the game's destination lookup and area tag colors. This includes repair/depot overrides, so a locomotive assigned to a repair destination is colored too. Cars at their destination retain 85% of the base RGB color, rather than the native half-strength dimming. Vehicles without a destination use opaque `#ddd`. Colors/identifiers and consist membership refresh twice per second; projected positions update every visible frame. No track colors or extra switch arrows are added.

The upper-right dropdown has three persistent modes:

- **Locomotives only**: no wagon or caboose icons.
- **Locomotives + caboose**: locomotive and caboose icons, useful for the whole-map overview.
- **All vehicles** (default): wagons are sampled per mechanically coupled consist according to projected car spacing. Close zoom shows every wagon; farther out it shows every second, third, etc. Cabooses and the selected car are retained. Standalone cars are retained. Locomotives always render last.

The filter and **Follow selected car** preference are stored using the game's local map preferences. Following centers the map on `TrainController.SelectedCar`, including when selection changes; with no selected car it leaves the current center. Following works while the map is open, independently of vehicle-layer visibility. Uncheck it to pan freely.

## Switch Save / Restore

The upper-right map panel replaces the standalone SwitchNormalizer toolbar icon. The buttons are separate actions, without a Shift modifier:

- **Save** stores only IDs of reversed/alternate switches; all other switches are implicitly normal.
- **Restore** reloads the profile and requests each required switch change through the native API. Occupied/blocked switches, locked CTC switches and unauthorized requests are skipped. The status line reports requests and skipped switches; native host validation still applies.

The profile remains at `Documents\Railroader\ModSettings\czbuchi\switch-normalizer.json`, using Windows' actual Documents folder. Existing native SwitchNormalizer profiles work directly. Saves are atomic, use UTF-8 with BOM and CRLF, and retain the prior file as `.bak`. Corrupt/unreadable profiles abort Restore and are not overwritten. As in SwitchNormalizer, an empty or missing profile means all switches are normal. No legacy RailForge import is performed.

SwitchNormalizer source/build and its old toolbar are retired from this branch. Remove its installed DLL pack separately; installing this ZIP does not delete other mods. This mod is optional in multiplayer: markers/preferences/profiles are local, while Restore uses native authorized switch request messages and host authority.

## Build and checks

```powershell
dotnet publish WaybillMap/WaybillMap.csproj -c Release
dotnet run --project tests/WaybillMap.Tests -c Release -- 'C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed' WaybillMap/bin/Release/CzBuCHi.WaybillMap.dll
```

Override GameDir through Paths.user or `-p:GameDir=...` if needed. Publishing creates `bin/CzBuCHi.WaybillMap_1.3.0.zip`. It contains only this mod's DLL, Info.json, README and LICENSE; game, Harmony and Newtonsoft DLLs are supplied by the game.

Automated checks cover fixed-size projection through normal/flipped UVs, clipping/culling, label orientation, text contrast, gentle spotted dimming, the three filters and zoom sampling. Migrated switch-profile tests cover reversed-ID normalization, backups, corrupt-file rejection, atomic write failures and encoding. Metadata checks validate installed API targets, native lifecycle, map controls, following and authorized switch requests. These tests do not run Unity or prove UI layout/rendering or original-icon restoration.

In game, check:

1. Compare vehicle icon sizes across zoom levels and against each other; inspect normal/spotted colors and a locomotive assigned to a depot.
2. Test all three vehicle filters, automatic wagon sampling, cabooses and selected-car retention.
3. Follow a moving selected car, select another car, close/reopen the map, then uncheck follow and pan.
4. Save switch positions, change some, then Restore. Check occupied switches, CTC locks and client permissions. Reuse an existing native SwitchNormalizer profile.
5. Reload the world and disable/re-enable the mod. Check panel cleanup and restoration of vanilla locomotive graphics.

Diagnostic prefix: `[WaybillMap]` plus the native mod-context log.
