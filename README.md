# Railroader Mods - experimental

This orphan branch contains native mods for Railroader 0.6. Legacy RailForge mods and their history remain on `master` (checkpoint `575e7e2`). The empty root of this branch is `7ea4503`.

## Switch Normalizer 3.0.0

Native DLL mod with a toolbar icon. Click to restore switch positions; Shift-click to save them. Requires no RailForge, UMM, or Harmony.

See [installation, profile location, and checks](SwitchNormalizer/README.md).

Build from the repository root:

```powershell
dotnet publish SwitchNormalizer/SwitchNormalizer.csproj -c Release
dotnet run --project tests/SwitchNormalizer.Tests -c Release -- SwitchNormalizer/bin/Release/CzBuCHi.SwitchNormalizer.dll
```

Default game location: `C:\Steam\steamapps\common\Railroader`. Override `GameDir` in an untracked `Paths.user` MSBuild project or pass `-p:GameDir="D:\path\Railroader"`.

Build targets .NET Framework 4.7.2 / C# 9 and uses the installed game's framework references because Unity 6 exposes newer framework types. Game DLLs are not distributed.

ZIP output: `bin/CzBuCHi.SwitchNormalizer_3.0.0.zip`. Publishing only builds the package; it does not install or launch the game.
