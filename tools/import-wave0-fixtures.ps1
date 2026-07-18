[CmdletBinding()]
param(
    [string]$MappingToolsDataPath = 'C:\Users\Olivier\AppData\Local\Mapping Tools',
    [string]$OsuSongsPath = 'C:\Users\Olivier\AppData\Local\osu!\Songs'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path $repositoryRoot 'tests\fixtures\wave0'

function Copy-SanitizedJson {
    param(
        [Parameter(Mandatory)] [string]$Source,
        [Parameter(Mandatory)] [string]$Destination
    )

    $content = Get-Content -Raw -LiteralPath $Source
    $content = $content -replace 'C:\\\\Users\\\\Olivier', '%USERPROFILE%'
    $content = $content -replace 'C:\\Users\\Olivier', '%USERPROFILE%'
    $content = $content -replace 'osu!\.Olivier\.cfg', 'osu!.User.cfg'
    $destinationDirectory = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    [System.IO.File]::WriteAllText($Destination, $content, [System.Text.UTF8Encoding]::new($false))
}

function Copy-Fixture {
    param(
        [Parameter(Mandatory)] [string]$Source,
        [Parameter(Mandatory)] [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Fixture source does not exist: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

function Write-Utf8Json {
    param(
        [Parameter(Mandatory)] [object]$Value,
        [Parameter(Mandatory)] [string]$Destination
    )

    $destinationDirectory = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    $json = $Value | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($Destination, "$json`n", [System.Text.UTF8Encoding]::new($false))
}

$projectFiles = @(
    'mapcleanerproject.json',
    'propertytransformerproject.json',
    'timingcopierproject.json',
    'timinghelperproject.json',
    'slidermergerproject.json',
    'slidercompletionatorproject.json',
    'sliderpicturatorproject.json',
    'rhythmguideproject.json',
    'hitsoundcopierproject.json',
    'hspreviewproject.json',
    'metadataproject.json',
    'slideratorproject.json',
    'tumourgeneratorproject.json',
    'combocolourproject.json',
    'mapsetmergerproject.json',
    'snappingtoolsproject.json',
    'hsstudioproject.json',
    'patterngalleryproject.json'
)

foreach ($projectFile in $projectFiles) {
    Copy-SanitizedJson `
        -Source (Join-Path $MappingToolsDataPath $projectFile) `
        -Destination (Join-Path $fixtureRoot "projects\$projectFile")
}

Copy-SanitizedJson `
    -Source (Join-Path $MappingToolsDataPath 'config.json') `
    -Destination (Join-Path $fixtureRoot 'settings\legacy-config.json')

$patternSource = Join-Path $MappingToolsDataPath 'Pattern Gallery Projects\850qxDWVPzx9BuvDIA7i'
Copy-SanitizedJson `
    -Source (Join-Path $patternSource 'project.json') `
    -Destination (Join-Path $fixtureRoot 'patterns\legacy-collection\project.json')
$patternFile = Get-ChildItem -LiteralPath (Join-Path $patternSource 'Pattern Files') -File -Filter '*.osu' | Select-Object -First 1
Copy-Fixture `
    -Source $patternFile.FullName `
    -Destination (Join-Path $fixtureRoot "patterns\legacy-collection\Pattern Files\$($patternFile.Name)")

Copy-Fixture `
    -Source (Join-Path $MappingToolsDataPath 'Exports\drum-hitnormal60.wav') `
    -Destination (Join-Path $fixtureRoot 'audio\drum-hitnormal60.wav')
Copy-Fixture `
    -Source (Join-Path $MappingToolsDataPath 'Exports\soft-hitwhistle6.ogg') `
    -Destination (Join-Path $fixtureRoot 'audio\soft-hitwhistle6.ogg')

$beatmaps = @(
    @{
        Source = '1002019 SAMString - Forget The Promise\SAMString - Forget The Promise (DeviousPanda) [Elysium].osu'
        Destination = 'beatmaps\standard-feature-rich.osu'
    },
    @{
        Source = "100019 Owl City & Carly Rae Jepsen - Good Time\Owl City & Carly Rae Jepsen - Good Time (Gero) [Mancuso's Muzukashii].osu"
        Destination = 'beatmaps\taiko.osu'
    },
    @{
        Source = "1002819 Roselia - MIIRO\Roselia - MIIRO (-Mikan) [Lacrima's PLATTER].osu"
        Destination = 'beatmaps\catch.osu'
    },
    @{
        Source = "1000722 Ohara Yuiko - Zero Centimeters (TV Size)\Ohara Yuiko - Zero Centimeters (TV Size) (-Mikan) [422's EZ].osu"
        Destination = 'beatmaps\mania.osu'
    },
    @{
        Source = "1000338 Thaehan - Kawaii\Thaehan - Kawaii (Cris-) [Normal].osu"
        Destination = 'mapsets\multi-difficulty\normal.osu'
    },
    @{
        Source = "1000338 Thaehan - Kawaii\Thaehan - Kawaii (Cris-) [Walao's Expert].osu"
        Destination = 'mapsets\multi-difficulty\expert.osu'
    }
)

foreach ($beatmap in $beatmaps) {
    Copy-Fixture `
        -Source (Join-Path $OsuSongsPath $beatmap.Source) `
        -Destination (Join-Path $fixtureRoot $beatmap.Destination)
}

$destructiveFeatures = @(
    @{ Id = 'map-cleaner'; Name = 'Map Cleaner'; Project = 'mapcleanerproject.json' },
    @{ Id = 'metadata-manager'; Name = 'Metadata Manager'; Project = 'metadataproject.json' },
    @{ Id = 'property-transformer'; Name = 'Property Transformer'; Project = 'propertytransformerproject.json' },
    @{ Id = 'timing-copier'; Name = 'Timing Copier'; Project = 'timingcopierproject.json' },
    @{ Id = 'timing-helper'; Name = 'Timing Helper'; Project = 'timinghelperproject.json' },
    @{ Id = 'rhythm-guide'; Name = 'Rhythm Guide'; Project = 'rhythmguideproject.json' },
    @{ Id = 'hitsound-preview'; Name = 'Hitsound Preview Helper'; Project = 'hspreviewproject.json' },
    @{ Id = 'hitsound-copier'; Name = 'Hitsound Copier'; Project = 'hitsoundcopierproject.json' },
    @{ Id = 'slider-completionator'; Name = 'Slider Completionator'; Project = 'slidercompletionatorproject.json' },
    @{ Id = 'slider-merger'; Name = 'Slider Merger'; Project = 'slidermergerproject.json' },
    @{ Id = 'slider-picturator'; Name = 'Slider Picturator'; Project = 'sliderpicturatorproject.json' },
    @{ Id = 'sliderator'; Name = 'Sliderator'; Project = 'slideratorproject.json' },
    @{ Id = 'tumour-generator'; Name = 'Tumour Generator 2'; Project = 'tumourgeneratorproject.json' },
    @{ Id = 'combo-colour-studio'; Name = 'Combo Colour Studio'; Project = 'combocolourproject.json' },
    @{ Id = 'pattern-gallery'; Name = 'Pattern Gallery'; Project = 'patterngalleryproject.json' },
    @{ Id = 'mapset-merger'; Name = 'Mapset Merger'; Project = 'mapsetmergerproject.json' },
    @{ Id = 'hitsound-studio'; Name = 'Hitsound Studio'; Project = 'hsstudioproject.json' },
    @{ Id = 'auto-fail-detector'; Name = 'Auto-fail Detector'; Project = $null }
)

foreach ($feature in $destructiveFeatures) {
    $record = [ordered]@{
        id = "TR-$($feature.Id)"
        feature = $feature.Name
        status = 'pending-capture'
        seedInput = '../../../../Mapping_Tools_Tests/Resources/ComplicatedTestMap.osu'
        options = if ($feature.Project) { "../projects/$($feature.Project)" } else { $null }
        expectedOutput = $null
        legacyVersion = '1.12.30'
        reviewer = $null
        reviewedOn = $null
        notes = 'Run the legacy feature against a disposable copy, then record and review the exact output.'
    }
    $recordPath = Join-Path $fixtureRoot "transformations\$($feature.Id).json"
    if (-not (Test-Path -LiteralPath $recordPath)) {
        Write-Utf8Json -Value $record -Destination $recordPath
    }
}

$fixtures = [System.Collections.Generic.List[object]]::new()
function Add-FixtureManifestEntry {
    param(
        [Parameter(Mandatory)] [string]$Id,
        [Parameter(Mandatory)] [string]$Group,
        [Parameter(Mandatory)] [string]$RelativePath,
        [Parameter(Mandatory)] [string]$Purpose
    )

    $absolutePath = [System.IO.Path]::GetFullPath((Join-Path $fixtureRoot $RelativePath))
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "Manifest fixture does not exist: $absolutePath"
    }

    $fixtures.Add([ordered]@{
        id = $Id
        group = $Group
        path = $RelativePath.Replace('\', '/')
        sha256 = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash
        purpose = $Purpose
    })
}

Add-FixtureManifestEntry 'BM-STD-RICH-001' 'beatmap' 'beatmaps\standard-feature-rich.osu' 'Real-world standard map with bookmarks, inherited timing, samples, colours, and complex hit objects.'
Add-FixtureManifestEntry 'BM-TAIKO-001' 'beatmap' 'beatmaps\taiko.osu' 'Real-world taiko-mode map.'
Add-FixtureManifestEntry 'BM-CATCH-001' 'beatmap' 'beatmaps\catch.osu' 'Real-world catch-mode map with bookmarks.'
Add-FixtureManifestEntry 'BM-MANIA-001' 'beatmap' 'beatmaps\mania.osu' 'Real-world mania-mode map with bookmarks.'
Add-FixtureManifestEntry 'BM-COMPACT-001' 'beatmap' '../../../Mapping_Tools_Tests/Resources/ComplicatedTestMap.osu' 'Compact standard map used by legacy parser and round-trip tests.'
Add-FixtureManifestEntry 'BM-EMPTY-001' 'beatmap' '../../../Mapping_Tools_Tests/Resources/EmptyTestMap.osu' 'Valid empty standard map for empty-selection and no-op cases.'
Add-FixtureManifestEntry 'SB-LARGE-001' 'storyboard' '../../../Mapping_Tools_Tests/Resources/TestStoryboard.osb' 'Large real-world storyboard command and serialization baseline.'
Add-FixtureManifestEntry 'MAPSET-MULTI-001-N' 'mapset' 'mapsets\multi-difficulty\normal.osu' 'Normal difficulty in a real-world shared mapset.'
Add-FixtureManifestEntry 'MAPSET-MULTI-001-X' 'mapset' 'mapsets\multi-difficulty\expert.osu' 'Expert difficulty in the same real-world shared mapset.'
Add-FixtureManifestEntry 'PAT-COLLECTION-001' 'pattern' 'patterns\legacy-collection\project.json' 'Legacy Pattern Gallery collection metadata.'
$importedPattern = Get-ChildItem -LiteralPath (Join-Path $fixtureRoot 'patterns\legacy-collection\Pattern Files') -File -Filter '*.osu' | Select-Object -First 1
$patternRelativePath = $importedPattern.FullName.Substring($fixtureRoot.Length).TrimStart('\')
Add-FixtureManifestEntry 'PAT-FILE-001' 'pattern' $patternRelativePath 'Loose pattern beatmap referenced by the legacy collection.'
Add-FixtureManifestEntry 'AUD-WAV-001' 'audio' 'audio\drum-hitnormal60.wav' 'Small generated hitsound sample in WAV format.'
Add-FixtureManifestEntry 'AUD-OGG-001' 'audio' 'audio\soft-hitwhistle6.ogg' 'Small generated hitsound sample in OGG format.'
Add-FixtureManifestEntry 'SET-LEGACY-001' 'settings' 'settings\legacy-config.json' 'Sanitized representative legacy settings including paths, favorites, bounds, and hotkeys.'
Add-FixtureManifestEntry 'SET-CORRUPT-001' 'settings' 'settings\corrupt.json' 'Intentionally malformed settings input.'
Add-FixtureManifestEntry 'FAILURES-001' 'platform-failure' 'platform-failures\scenarios.json' 'Stable IDs for unavailable editor, denied storage, missing media, offline network, and picker cancellation.'
Add-FixtureManifestEntry 'PROJECT-CORRUPT-001' 'project' 'projects\corrupt.json' 'Intentionally malformed legacy project JSON.'

foreach ($projectFile in $projectFiles) {
    $projectId = [System.IO.Path]::GetFileNameWithoutExtension($projectFile).ToUpperInvariant()
    Add-FixtureManifestEntry "PROJECT-$projectId" 'project' "projects\$projectFile" "Sanitized real-world legacy $projectFile configuration."
}

foreach ($feature in $destructiveFeatures) {
    Add-FixtureManifestEntry "TR-$($feature.Id)" 'transformation' "transformations\$($feature.Id).json" "Legacy before/after capture record for $($feature.Name)."
}

Get-ChildItem -LiteralPath (Join-Path $fixtureRoot 'transformations') -File -Filter '*.options.json' | ForEach-Object {
    $featureId = $_.BaseName -replace '\.options$', ''
    $relativePath = "transformations\$($_.Name)"
    Add-FixtureManifestEntry "TR-$featureId-options" 'transformation' $relativePath "Exact legacy $featureId capture settings."
}
Get-ChildItem -LiteralPath (Join-Path $fixtureRoot 'transformations') -File -Filter '*-report.md' | ForEach-Object {
    $featureId = $_.BaseName -replace '-report$', ''
    $relativePath = "transformations\$($_.Name)"
    Add-FixtureManifestEntry "TR-$featureId-report" 'transformation' $relativePath "Semantic comparison and capture evidence for $featureId."
}
Get-ChildItem -LiteralPath (Join-Path $fixtureRoot 'transformations') -File -Filter '*.output.json' | ForEach-Object {
    $featureId = $_.BaseName -replace '\.output$', ''
    $relativePath = "transformations\$($_.Name)"
    Add-FixtureManifestEntry "TR-$featureId-output-manifest" 'transformation' $relativePath "Exact legacy multi-file output manifest for $featureId."
}
Get-ChildItem -LiteralPath (Join-Path $fixtureRoot 'transformations\expected') -File -Filter '*.osu' | ForEach-Object {
    $featureId = $_.BaseName
    $relativePath = "transformations\expected\$($_.Name)"
    Add-FixtureManifestEntry "TR-$featureId-output" 'transformation' $relativePath "Exact legacy $featureId output."
}

$manifest = [ordered]@{
    schemaVersion = 1
    generatedOn = '2026-07-18'
    safety = 'Originals are read-only. Copy fixtures to a per-run directory before mutation.'
    fixtures = $fixtures
    destructiveFeatures = @($destructiveFeatures | ForEach-Object {
        [ordered]@{ id = $_.Id; baselineFixtureId = "TR-$($_.Id)" }
    })
}
Write-Utf8Json -Value $manifest -Destination (Join-Path $fixtureRoot 'manifest.json')

Write-Host "Imported Wave 0 fixtures into $fixtureRoot"
