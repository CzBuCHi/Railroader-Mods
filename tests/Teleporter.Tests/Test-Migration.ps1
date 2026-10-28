#requires -Version 7.3
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot '..\..\Teleporter\Migrate-RailForgeProfile.ps1'
$workRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\work'))
$folder = Join-Path $workRoot ('MigrationTests-' + [Guid]::NewGuid().ToString('N'))
[void] [IO.Directory]::CreateDirectory($folder)
$script:checks = 0
function Check([bool] $Value, [string] $Message) {
    if (-not $Value) { throw $Message }
    $script:checks++
}
function Write-Json([string] $Path, $Value) {
    $json = (ConvertTo-Json -InputObject $Value -Depth 20).Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n"
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($true))
}
function Expect-Rejected([scriptblock] $Action, [string] $Message) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    Check $failed $Message
}
try {
    $source = Join-Path $folder 'rf.json'
    $destination = Join-Path $folder 'new\teleporter.json'
    $profile = @{
        Groups = @(
            @{
                Name = 'Nádraží'
                Locations = @{}
            }
            @{ Name = 'Empty'; Locations = @{} }
        )
        AutoOpenTeleporterWindow = $true
        UnusedRailForgeField = 'discarded'
    }
    # Construct a case-sensitive dictionary; PowerShell literal maps reject View/view.
    $locations = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    $locations.Add('View', @{ Position = @{ X = 12.5; Y = 4; Z = -8 }; Rotation = @{ X = 20; Y = 135; Z = 0 }; Distance = 40 })
    $locations.Add('view', @{ Position = @{ X = 2; Y = 4; Z = 8 }; Rotation = @{ X = 10; Y = 90; Z = 0 }; Distance = 0; FirstPerson = $true })
    $profile.Groups[0].Locations = $locations
    Write-Json $source $profile
    $sourceHash = (Get-FileHash -LiteralPath $source).Hash
    & $script -SourcePath $source -DestinationPath $destination -WhatIf
    Check (-not (Test-Path -LiteralPath (Split-Path $destination))) 'WhatIf must not create destination directory'
    $result = & $script -SourcePath $source -DestinationPath $destination
    $converted = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($destination)) -AsHashtable
    Check ($result.Groups -eq 2 -and $result.Locations -eq 2) 'Migration reports counts'
    Check ($converted.Groups[0].Name -ceq 'Nádraží' -and $converted.Groups[1].Name -ceq 'Empty') 'Group order and Unicode retained'
    Check ($converted.Groups[0].Locations.Count -eq 2) 'Case-sensitive location names retained'
    Check ($converted.Groups[0].Locations['View'].FirstPerson -eq $false) 'Legacy missing FirstPerson defaults to tracking'
    Check ($converted.Groups[0].Locations['view'].FirstPerson -eq $true) 'First-person mode retained'
    Check ($converted.Groups[0].Locations['View'].Position.X -eq 12.5 -and $converted.Groups[0].Locations['View'].Rotation.Y -eq 135 -and $converted.Groups[0].Locations['View'].Distance -eq 40) 'Position, rotation, and distance retained'
    Check ($converted.AutoOpenTeleporterWindow -eq $true -and -not $converted.Contains('UnusedRailForgeField')) 'Known fields normalized'
    $bytes = [IO.File]::ReadAllBytes($destination)
    Check ($bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191) 'Output has UTF-8 BOM'
    Check (-not [IO.File]::ReadAllText($destination).Replace("`r`n", '').Contains("`n")) 'Output has CRLF'
    $destinationHash = (Get-FileHash -LiteralPath $destination).Hash
    Expect-Rejected { & $script -SourcePath $source -DestinationPath $destination } 'Existing destination requires Force'
    Check ((Get-FileHash -LiteralPath $destination).Hash -eq $destinationHash) 'Rejected overwrite preserves destination'
    $result = & $script -SourcePath $source -DestinationPath $destination -Force
    Check ((Get-FileHash -LiteralPath $result.Backup).Hash -eq $destinationHash) 'Force preserves exact previous file in backup'
    Expect-Rejected { & $script -SourcePath $source -DestinationPath $source -Force } 'Source cannot be destination'
    Check ((Get-FileHash -LiteralPath $source).Hash -eq $sourceHash) 'Original RF file unchanged'
    foreach ($bad in @(
        @{ Groups = $null },
        @{ Groups = @(@{ Name = 'Bad'; Locations = @{ X = @{ Position = @{ X = 'wrong'; Y = 0; Z = 0 }; Rotation = @{ X = 0; Y = 0; Z = 0 }; Distance = 5 } } }) },
        @{ Groups = @(@{ Name = 'Bad'; Locations = @{ X = @{ Position = @{ X = 0; Y = 0; Z = 0 }; Rotation = @{ X = 0; Y = 0; Z = 0 }; Distance = -1 } } }) },
        @{ Groups = @(@{ Name = 'Bad'; Locations = @{} }, @{ Name = 'Bad'; Locations = @{} }) },
        @{ Groups = @(); AutoOpenTeleporterWindow = 'false' }
    )) {
        $badSource = Join-Path $folder 'bad.json'
        Write-Json $badSource $bad
        $before = (Get-FileHash -LiteralPath $destination).Hash
        Expect-Rejected { & $script -SourcePath $badSource -DestinationPath $destination -Force } 'Malformed source rejected'
        Check ((Get-FileHash -LiteralPath $destination).Hash -eq $before) 'Validation failure preserves destination'
    }
    $emptySource = Join-Path $folder 'empty.json'
    $emptyDestination = Join-Path $folder 'empty-new.json'
    Write-Json $emptySource @{ Groups = @() }
    & $script -SourcePath $emptySource -DestinationPath $emptyDestination | Out-Null
    $empty = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($emptyDestination)) -AsHashtable
    Check ($empty.Groups.Count -eq 0 -and $empty.AutoOpenTeleporterWindow -eq $false) 'Empty profile and missing preference supported'
    $game = Join-Path $folder 'game'
    $defaultSource = Join-Path $game 'Mods\RTM.RailForge\LegacySettings\CzBuCHi.Teleporter--CzBuCHi.Teleporter.json'
    [void] [IO.Directory]::CreateDirectory((Split-Path $defaultSource))
    [IO.File]::Copy($source, $defaultSource)
    $result = & $script -GameDir $game -DestinationPath (Join-Path $folder 'game-new.json')
    Check ($result.Locations -eq 2) 'GameDir source discovery works'
    Check (@(Get-ChildItem -LiteralPath $folder -Recurse -Filter '*.tmp').Count -eq 0) 'No temporary files remain'
    "PASS: $script:checks standalone migration checks."
} finally {
    $resolved = [IO.Path]::GetFullPath($folder)
    if (-not $resolved.StartsWith($workRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Test cleanup escaped work directory.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
