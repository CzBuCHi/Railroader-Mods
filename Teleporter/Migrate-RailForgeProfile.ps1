#requires -Version 7.3
<#
.SYNOPSIS
Converts a RailForge Teleporter profile to the native Teleporter profile.
.DESCRIPTION
Run once with Railroader closed. The source is never changed. Existing destination
files require -Force and are backed up before replacement. -WhatIf validates and
previews without writing. Requires PowerShell 7.3 or later (pwsh).
.EXAMPLE
pwsh -File .\Migrate-RailForgeProfile.ps1
.EXAMPLE
pwsh -File .\Migrate-RailForgeProfile.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Railroader' -WhatIf
.EXAMPLE
pwsh -File .\Migrate-RailForgeProfile.ps1 -SourcePath 'D:\backup\Teleporter.json' -DestinationPath 'D:\profiles\teleporter.json'
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $GameDir = 'C:\Steam\steamapps\common\Railroader',
    [string] $SourcePath,
    [string] $DestinationPath,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Convert-Coordinate {
    param($Value, [string] $Label)
    if ($Value -isnot [long] -and $Value -isnot [int] -and $Value -isnot [double] -and $Value -isnot [decimal]) {
        throw "$Label must be a JSON number."
    }
    $number = [double] $Value
    if ([double]::IsNaN($number) -or [double]::IsInfinity($number) -or [Math]::Abs($number) -gt [float]::MaxValue) {
        throw "$Label must be a finite single-precision number."
    }
    return [float] $number
}

function Convert-Vector {
    param($Value, [string] $Label)
    if ($Value -isnot [System.Collections.IDictionary]) { throw "$Label must be a vector object." }
    $result = [ordered] @{}
    foreach ($axis in @('X', 'Y', 'Z')) {
        if (-not $Value.Contains($axis)) { throw "$Label.$axis is missing." }
        $result[$axis] = Convert-Coordinate $Value[$axis] "$Label.$axis"
    }
    return $result
}

function Read-Boolean {
    param([System.Collections.IDictionary] $Object, [string] $Key)
    if (-not $Object.Contains($Key)) { return $false }
    if ($Object[$Key] -isnot [bool]) { throw "$Key must be a JSON boolean." }
    return $Object[$Key]
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $GameDir 'Mods\RTM.RailForge\LegacySettings\CzBuCHi.Teleporter--CzBuCHi.Teleporter.json'
}
if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
    $documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
    if ([string]::IsNullOrWhiteSpace($documents)) { throw 'Documents directory was not found. Supply -DestinationPath.' }
    $DestinationPath = Join-Path $documents 'Railroader\ModSettings\czbuchi\teleporter.json'
}
$source = (Resolve-Path -LiteralPath $SourcePath).ProviderPath
$destination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DestinationPath)
if ([string]::Equals($source, $destination, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Source and destination must be different files.'
}
if (Test-Path -LiteralPath $destination) {
    if (-not [IO.File]::Exists($destination)) { throw 'Destination must be a file path.' }
    # Refuse links so neither -Force nor a backup path can indirectly replace the RF source.
    foreach ($itemPath in @($source, $destination)) {
        if ((Get-Item -LiteralPath $itemPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Source and existing destination must be ordinary files, not links.'
        }
    }
    if (-not $Force) { throw 'Destination already exists. Use -Force to replace it and create a backup.' }
}

$profile = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($source)) -AsHashtable -Depth 100
if ($profile -isnot [System.Collections.IDictionary] -or $profile['Groups'] -isnot [array]) {
    throw 'RF profile must contain a Groups array.'
}
$groups = [Collections.Generic.List[object]]::new()
$groupNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$locationCount = 0
foreach ($group in $profile['Groups']) {
    if ($group -isnot [System.Collections.IDictionary] -or $group['Name'] -isnot [string] -or
        [string]::IsNullOrWhiteSpace($group['Name']) -or $group['Locations'] -isnot [System.Collections.IDictionary]) {
        throw 'Each group needs a nonempty Name and a Locations object.'
    }
    if (-not $groupNames.Add($group['Name'])) { throw "Duplicate group name: $($group['Name'])" }
    # Generic Dictionary preserves names that differ only by case.
    $locations = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($entry in $group['Locations'].GetEnumerator()) {
        if ([string]::IsNullOrWhiteSpace($entry.Key) -or $entry.Value -isnot [System.Collections.IDictionary]) {
            throw 'Each saved location needs a nonempty name and a camera object.'
        }
        $location = $entry.Value
        $firstPerson = Read-Boolean $location 'FirstPerson'
        $distance = Convert-Coordinate $location['Distance'] 'Distance'
        if (-not $firstPerson -and $distance -lt 0) { throw 'Tracking camera Distance must not be negative.' }
        $locations.Add($entry.Key, [ordered] @{
            Position = Convert-Vector $location['Position'] 'Position'
            Rotation = Convert-Vector $location['Rotation'] 'Rotation'
            Distance = $distance
            FirstPerson = $firstPerson
        })
        $locationCount++
    }
    $groups.Add([ordered] @{ Name = $group['Name']; Locations = $locations })
}
$converted = [ordered] @{
    Groups = $groups.ToArray()
    AutoOpenTeleporterWindow = Read-Boolean $profile 'AutoOpenTeleporterWindow'
}
$json = (ConvertTo-Json -InputObject $converted -Depth 20).Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n"
$backup = $null
if ($PSCmdlet.ShouldProcess($destination, "Migrate $($groups.Count) groups and $locationCount locations from $source")) {
    [void] [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
    $temporary = $destination + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary, $json, [Text.UTF8Encoding]::new($true))
        if ([IO.File]::Exists($destination)) {
            if (-not $Force) { throw 'Destination appeared during migration. Nothing was replaced; rerun with -Force if intended.' }
            $backup = $destination + '.migration-' + [Guid]::NewGuid().ToString('N') + '.bak'
            [IO.File]::Replace($temporary, $destination, $backup)
        } else {
            [IO.File]::Move($temporary, $destination)
        }
    } finally {
        if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
    }
    [pscustomobject] @{
        Source = $source
        Destination = $destination
        Groups = $groups.Count
        Locations = $locationCount
        Backup = $backup
    }
}
