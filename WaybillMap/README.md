# WaybillMap 1.2.0

Standalone native DLL mod for Railroader 0.6.x. Identifier: `czbuchi/waybill-map`. Independent of Tweaks; no RailForge/UMM is required.

## Install and use

1. Close Railroader.
2. Extract the ZIP's `CzBuCHi.WaybillMap` folder into `Documents\Railroader\Mods`.
3. Enable Preferences > Features > Experimental > **Allow Unsafe DLL Mods** and restart if needed.
4. Enable **WaybillMap** in Manage Mods and apply.
5. Open the map. Its gear/**Display** menu includes **Waybill cars**, enabled by default. The game stores this choice in its existing map preferences; no custom configuration file or window is needed.

Every non-locomotive car in the logical world registry is included: freight cars, passenger cars, cabooses and tenders, whether coupled or parked and whether their detailed 3D model is loaded. Locomotives keep their native arrow and number and are drawn last, but their copied icon is reduced to fit within the actual engine length (at most 80% of vanilla size). While that compact copy is visible, the oversized native icon graphics are hidden. Their previous enabled states are restored when the layer/map is hidden, the world unloads or the mod is disabled. The original native icon retains its existing click handling. This adds local display only and is optional in multiplayer.

Car rectangles follow their projected physical length, leaving a coupler gap, with a consistent world-space width. Labels use reporting mark and road number on two centered rows; tenders use only the number. This follows MapEnhancer's car-length-based layout idea, implemented independently without its assets, track colors or switch arrows. Badges follow the car's orientation; text flips to remain readable and uses black or white according to contrast against the fill. They follow pan, zoom, resizing, UV orientation and floating world origin and are clipped to the map image. As the map zooms out, labels reduce to the road number alone, then disappear if even that no longer fits. Badges stay compact instead of widening over neighboring cars. Colors, numbers and the car list refresh every half second while the map is visible; positions update each frame. Nothing is scanned/drawn by this mod while the map or layer is hidden. Marker meshes are batched to avoid Unity UI's 16-bit vertex limit and text labels are pooled.

For cars with a waybill, colors come directly from the game's `UI.Tags.CarTagColor.For`, the same rule used by destination/waybill tags and waypoint car picking. This includes the destination area's tag color and native dimming when a car has reached its destination. A car without a waybill (including passenger cars) gets an opaque light gray `#ddd` fill. Existing game settings and routing rules determine the destination; this mod does not change waybills.

Markers do not intercept clicks or dragging; native map interactions still work underneath. There is no marker selection/teleport action in this first version. All overlays and the Display menu registration are removed on world unload/mod disable. Restart after replacing the DLL.

## Build and validation

```powershell
dotnet publish WaybillMap/WaybillMap.csproj -c Release
dotnet run --project tests/WaybillMap.Tests -c Release -- 'C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed' WaybillMap/bin/Release/CzBuCHi.WaybillMap.dll
```

Set GameDir via Paths.user or `-p:GameDir=...` if needed. Publishing creates `bin/CzBuCHi.WaybillMap_1.2.0.zip` without installing it or launching the game. The package contains only this mod's DLL, Info.json, README and LICENSE. No game/Harmony DLLs are shipped.

Tests exercise projection from camera viewport coordinates through the image's UV rectangle, aspect/resize, direction, offscreen rejection, edge overlap, invalid positions, physical car bounds and coupler gaps, locomotive fit scale, reporting-mark/number labels, readable text rotation and text contrast on light/dark/translucent fills. They also check the installed game API and compiled DLL's native lifecycle, map hook, registry/color/waybill/number references and absence of state-changing APIs. They do not run Unity or prove in-game rendering or visual-state restoration.

In-game checks: compare parked/moving and distant cars to their destination tags; change a waybill/destination and verify its color refreshes; check arrived and unassigned cars; pan, zoom, resize and move far enough to shift the world origin. Toggle Waybill cars, reload the world, disable/re-enable the mod and check no overlays are duplicated or left behind. Check that native track picking/teleporting and map dragging still work.

Diagnostic prefix: `[WaybillMap]`.
