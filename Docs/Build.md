# Build and configuration

## Requirements

Install the .NET SDK, Railroader, and RTM.RailForge. Mod assemblies target .NET Framework 4.8; tests target .NET 10. Game and RailForge DLLs are referenced from the installed game and are not included in packages.

## Local paths

Copy Paths.user.sample to Paths.user and edit GameDir:

    <Project>
      <PropertyGroup>
        <GameDir>D:\SteamLibrary\steamapps\common\Railroader</GameDir>
      </PropertyGroup>
    </Project>

Directory.Build.props imports Paths.user relative to its own directory. The fallback GameDir is C:\Steam\steamapps\common\Railroader. GameManagedDir defaults to GameDir/Railroader_Data/Managed; RailForgeDir defaults to GameDir/Mods/RTM.RailForge. Both may be overridden.

Paths.user is ignored by Git through *.user; Paths.user.sample is tracked. Explicit command-line GameDir takes precedence.

## Publish all mods

Run from the repository root:

    dotnet publish -c Release

Railroader-Mods.slnx contains the four mod projects. Directory.Build.targets runs the applicable checks, verifies the assembly/manifest/project versions, and creates these archives in the repository's bin directory:

- CzBuCHi.SwitchNormalizer_2.0.4.zip
- CzBuCHi.TurntableControl_2.0.4.zip
- CzBuCHi.Teleporter_2.0.5.zip
- CzBuCHi.Tweaks_2.0.22.zip

Plain dotnet publish also works, using the SDK's default configuration. Use -c Release for distribution. Publishing the same mod version replaces its ZIP; ZIPs for other versions are retained.

## Publish one mod or override the game path

    dotnet publish Tweaks/Tweaks.csproj -c Release
    dotnet publish -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Railroader"

To build without creating packages:

    dotnet build -c Release

To publish without the mod packaging target:

    dotnet publish -c Release -p:PackageMod=disable

## Package contents

Each ZIP contains Mods/CzBuCHi.<Mod>, its DLL, Definition.json, LICENSE, README.md, and docs/<Mod>.md. Tweaks also includes default PrankTexts.txt.

Only mod deliverables are included; game assemblies, RailForge.dll, debug symbols, and intermediate files are excluded. Close Railroader before extracting a package into the game directory, and preserve a customized PrankTexts.txt when updating Tweaks.

Publishing creates local archives. It does not copy files into the game or create GitHub releases.

## MSBuild integration

The mod projects set IsMod=true. Test projects default to false and do not create mod ZIPs. PackageMod defaults to enable and runs after Publish, rather than after an ordinary build.

The original GameAssembly and ModAssembly reference declarations remain supported. Time-based version generation has been removed: the project's Version must match Definition.json and the built DLL.

Staging is confined to each project's obj/<Configuration>/RailForgePackage directory. ZipDirectory creates the archive without a PowerShell dependency. The package target runs rotation checks for TurntableControl and the Tweaks checks before creating their ZIPs.

## Text files and validation

Authored text files use UTF-8 with BOM and CRLF. Build checks cannot replace testing Unity rendering, audio, collision handling, or multiplayer in the game.
