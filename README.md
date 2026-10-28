# Railroader Mods - experimental

This orphan branch contains native mods for Railroader 0.6. Legacy RailForge mods and their history remain on `master` (checkpoint `575e7e2`). The empty root of this branch is `7ea4503`.

## Switch Normalizer 3.0.0

Native DLL mod with a toolbar icon. Click to restore switch positions; Shift-click to save them. Requires no RailForge, UMM, or Harmony.

See [installation, profile location, and checks](SwitchNormalizer/README.md).

## Teleporter 3.0.1

Hybrid native DLL and Relay mod. The main Relay window uses the game's wagon location rows (destination icon + label); a separate Relay editor handles adding, replacing, deleting, and group order. The DLL handles exact camera bookmarks, local profiles, the toolbar, and small native UI adapters. See [Teleporter guide](Teleporter/README.md).

Build from the repository root:

```powershell
dotnet publish SwitchNormalizer/SwitchNormalizer.csproj -c Release
dotnet run --project tests/SwitchNormalizer.Tests -c Release -- SwitchNormalizer/bin/Release/CzBuCHi.SwitchNormalizer.dll
dotnet publish Teleporter/Teleporter.csproj -c Release
dotnet run --project tests/Teleporter.Tests -c Release -- Teleporter/Scripts/Main.rl "C:/Steam/steamapps/common/Railroader/Railroader_Data/Managed" Teleporter/bin/Release/CzBuCHi.Teleporter.dll
pwsh -NoProfile -File tests/Teleporter.Tests/Test-Migration.ps1
```

Default game location: `C:\Steam\steamapps\common\Railroader`. Override `GameDir` in an untracked `Paths.user` MSBuild project or pass `-p:GameDir="D:\path\Railroader"`.

SwitchNormalizer targets .NET Framework 4.7.2; Teleporter targets 4.8 to match the game's bundled Harmony assembly. Both use C# 9 and the installed game's framework references because Unity 6 exposes newer framework types. Game DLLs are not distributed.

ZIP output: `bin/CzBuCHi.SwitchNormalizer_3.0.0.zip`. Publishing only builds the package; it does not install or launch the game.

Teleporter ZIP output: `bin/CzBuCHi.Teleporter_3.0.1.zip`. The game already supplies its Harmony dependency; no separate loader or bundled Harmony copy is needed. RailForge migration is a one-time standalone script included in this ZIP; the DLL never reads RF configuration. See its guide for invocation.
