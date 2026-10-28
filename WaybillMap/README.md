# WaybillMap 1.5.0

Native Railroader 0.6.x map mod. Identifier: `czbuchi/waybill-map`. Includes the former SwitchNormalizer functionality. No RailForge/UMM is needed.

## Install

1. Close Railroader and disable/remove the separate SwitchNormalizer mod.
2. Extract `CzBuCHi.WaybillMap` from the ZIP into `Documents\Railroader\Mods`, replacing the previous WaybillMap version.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods** and restart if needed. Enable WaybillMap in Manage Mods.
4. Open the map. Map > Display > **Waybill cars** toggles the vehicle layer; the upper-right control panel provides switch tools, following and a vehicle filter.

## Vehicle layer and upper-right controls

All logical cars are available, including parked/distant vehicles without loaded detailed models. Vehicle rendering is now a direct port of MapEnhancer's native world-space MapIcon pipeline, rather than custom UI quads or a separate width/gap formula:

- Clone the game's locomotive MapIcon prefab.
- Use MapEnhancer's original `traincar.png` for wagons (embedded, byte-for-byte unchanged).
- Scale image/text children by 0.8; stretch wagon image Y by `carLength / 11`.
- Set wagon text to 19 units, fixed size, left alignment, auto-sized text container, centered local position and `<line-height=70%>` reporting mark/number rows. Engines and tenders use the number alone.
- Use native `MapIcon.SetZoom` and the map's actual IconScale. MapEnhancer's 180-degree text rotation rule applies only to this mod's own icons.
- Place engines on the upper world-space layer and wagons below them. The two depth layers are anchored below the map camera so terrain height cannot push a marker above the camera. The map render texture provides viewport clipping.

The former custom track-relative sizing, UI text labels and eight-point gap adjustment are removed. Native track styles and switch markers are unchanged. The ported rendering logic and asset are licensed under MIT; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

A steam locomotive's connected rear tender is still merged into one marker: combined center and longer image, engine number/color, no second tender icon. Unmerged engines keep MapEnhancer's 0.8 child scale; merged engines extend that long axis by the combined/engine length ratio. A detached tender is a normal standalone wagon. Either tender orientation is supported. The original native locomotive graphics are hidden only while this layer is active and restored on hide/unload; native click targets are preserved. New markers do not intercept input.

Destination colors use the game's destination lookup and area tag colors. This includes repair/depot overrides, so a locomotive assigned to a repair destination is colored too. Cars at their destination retain 85% of the base RGB color, rather than the native half-strength dimming. Vehicles without a destination use opaque `#ddd`. Colors/identifiers and consist membership refresh twice per second; world positions update every visible frame. No track colors or extra switch arrows are added.

The upper-right panel follows this layout: Switch positions + Save/Restore on row one; Render + dropdown on row two; Follow selected car under the dropdown. The dropdown has three persistent modes:

- **Locomotives only**: no wagon or caboose icons.
- **Locomotives + caboose**: locomotive and caboose icons, useful for the whole-map overview.
- **All vehicles** (default): wagons are sampled per mechanically coupled consist when native icon extents overlap. This uses the native image height and zoom; image dimensions are never stretched to manufacture a gap. Cabooses and the selected car are retained. Standalone cars are retained. Locomotives always render last.

The filter and **Follow selected car** preference are stored using the game's local map preferences. Following centers the map on `TrainController.SelectedCar`, including when selection changes; with no selected car it leaves the current center. Following works while the map is open, independently of vehicle-layer visibility. Dragging the map automatically unchecks Follow selected car before the native pan is applied. A click or zoom wheel alone does not disable follow.

## Switch Save / Restore

The upper-right map panel replaces the standalone SwitchNormalizer toolbar icon. The buttons are separate actions, without a Shift modifier:

- **Save** stores only IDs of reversed/alternate switches; all other switches are implicitly normal.
- **Restore** reloads the profile and requests each required switch change through the native API. Occupied/blocked switches, locked CTC switches and unauthorized requests are skipped. Hover over the Switch positions heading for the most recent result; native host validation still applies.

The profile remains at `Documents\Railroader\ModSettings\czbuchi\switch-normalizer.json`, using Windows' actual Documents folder. Existing native SwitchNormalizer profiles work directly. Saves are atomic, use UTF-8 with BOM and CRLF, and retain the prior file as `.bak`. Corrupt/unreadable profiles abort Restore and are not overwritten. As in SwitchNormalizer, an empty or missing profile means all switches are normal. No legacy RailForge import is performed.

SwitchNormalizer source/build and its old toolbar are retired from this branch. Remove its installed DLL pack separately; installing this ZIP does not delete other mods. This mod is optional in multiplayer: markers/preferences/profiles are local, while Restore uses native authorized switch request messages and host authority.

## Build and checks

```powershell
dotnet publish WaybillMap/WaybillMap.csproj -c Release
dotnet run --project tests/WaybillMap.Tests -c Release -- 'C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed' WaybillMap/bin/Release/CzBuCHi.WaybillMap.dll
```

Override GameDir through Paths.user or `-p:GameDir=...` if needed. Publishing creates `bin/CzBuCHi.WaybillMap_1.5.0.zip`. It contains this mod's DLL, Info.json, README, LICENSE and third-party notices; game, Harmony and Newtonsoft DLLs are supplied by the game.

Automated checks compare the source rendering constants, line layout, embedded sprite SHA-256, filters, tender pairing, mild spotted colors and contrast. Migrated switch-profile tests cover backups, corruption, atomic failures and encoding. Installed-API checks cover native prefabs, zoom/rotation, map dragging, lifecycle, following and authorized switch requests. These tests do not run Unity or prove in-game appearance.

In game, check:

1. Compare vehicle icon sizes across zoom levels and against each other; inspect normal/spotted colors and a locomotive assigned to a depot.
2. Test all three vehicle filters, automatic wagon sampling, cabooses and selected-car retention.
3. Follow a moving selected car, then drag: follow must uncheck immediately and the map must pan without snapping back. Check that clicks and wheel zoom retain follow.
4. Save switch positions, change some, then Restore. Check occupied switches, CTC locks and client permissions. Reuse an existing native SwitchNormalizer profile.
5. Reload the world and disable/re-enable the mod. Check panel cleanup and restoration of vanilla locomotive graphics.

Diagnostic prefix: `[WaybillMap]` plus the native mod-context log.
