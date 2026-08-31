[CmdletBinding()]
param()

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "mapping-tools-release-layout-$([Guid]::NewGuid())"
$assetDirectory = Join-Path $testRoot 'release-assets'
$validator = Join-Path $PSScriptRoot 'validate-release-layout.ps1'

try {
    New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null

    $publishes = @(
        @{ Rid = 'win-x86'; Executable = 'Mapping Tools.exe'; Asset = 'mapping-tools-windows-x86.zip' },
        @{ Rid = 'win-x64'; Executable = 'Mapping Tools.exe'; Asset = 'mapping-tools-windows-x64.zip' },
        @{ Rid = 'linux-x64'; Executable = 'Mapping Tools'; Asset = 'mapping-tools-linux-x64.zip' },
        @{ Rid = 'linux-arm64'; Executable = 'Mapping Tools'; Asset = 'mapping-tools-linux-arm64.zip' },
        @{ Rid = 'osx-x64'; Executable = 'Mapping Tools'; Asset = 'mapping-tools-osx-x64.zip' },
        @{ Rid = 'osx-arm64'; Executable = 'Mapping Tools'; Asset = 'mapping-tools-osx-arm64.zip' }
    )

    $publishDirectories = @()
    $archives = @()
    $executableNames = @()
    $assetNames = @()
    $archiveExecutableNames = @()

    foreach ($publish in $publishes) {
        $directory = Join-Path $testRoot "Mapping_Tools.Desktop/bin/Release/net10.0/$($publish.Rid)/publish"
        New-Item -ItemType Directory -Path $directory -Force | Out-Null

        @(
            $publish.Executable,
            'Mapping_Tools.Desktop.dll',
            'Mapping_Tools.Desktop.deps.json',
            'Mapping_Tools.Desktop.runtimeconfig.json'
        ) | ForEach-Object {
            Set-Content -LiteralPath (Join-Path $directory $_) -Value 'fixture' -NoNewline
        }

        $archive = Join-Path $assetDirectory $publish.Asset
        if ($publish.Rid -like 'osx-*') {
            $bundle = Join-Path $testRoot 'Mapping Tools.app/Contents/MacOS'
            New-Item -ItemType Directory -Path $bundle -Force | Out-Null
            Get-ChildItem -LiteralPath $directory -File | ForEach-Object {
                Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $bundle $_.Name)
            }
            Set-Content -LiteralPath (Join-Path $testRoot 'Mapping Tools.app/Contents/Info.plist') -Value '<plist />' -NoNewline
            Compress-Archive -Path (Join-Path $testRoot 'Mapping Tools.app') -DestinationPath $archive -Force
            $archiveExecutableNames += 'Mapping Tools.app/Contents/MacOS/Mapping Tools'
        }
        else {
            $files = Get-ChildItem -LiteralPath $directory -File | Sort-Object Name |
                Select-Object -ExpandProperty FullName
            Compress-Archive -Path $files -DestinationPath $archive -Force
            $archiveExecutableNames += $publish.Executable
        }

        $publishDirectories += $directory
        $archives += $archive
        $executableNames += $publish.Executable
        $assetNames += $publish.Asset
    }

    $compatibilityX86 = Join-Path $assetDirectory 'release.zip'
    $compatibilityX64 = Join-Path $assetDirectory 'release_x64.zip'
    Copy-Item $archives[0] $compatibilityX86
    Copy-Item $archives[1] $compatibilityX64

    $installerX86 = Join-Path $testRoot 'mapping_tools_installer_x86.exe'
    $installerX64 = Join-Path $testRoot 'mapping_tools_installer_x64.exe'
    Set-Content -LiteralPath $installerX86 -Value 'fixture' -NoNewline
    Set-Content -LiteralPath $installerX64 -Value 'fixture' -NoNewline

    & $validator `
        -PublishDirectory $publishDirectories `
        -Archive $archives `
        -ExecutableName $executableNames `
        -AssetName $assetNames `
        -ArchiveExecutableName $archiveExecutableNames `
        -CompatibilityArchiveX86 $compatibilityX86 `
        -CompatibilityArchiveX64 $compatibilityX64 `
        -InstallerX86 $installerX86 `
        -InstallerX64 $installerX64

    Remove-Item -LiteralPath (Join-Path $publishDirectories[2] 'Mapping Tools')
    try {
        & $validator `
            -PublishDirectory $publishDirectories `
            -Archive $archives `
            -ExecutableName $executableNames `
            -AssetName $assetNames `
            -ArchiveExecutableName $archiveExecutableNames
        throw 'Validator accepted a publish missing its user-facing executable.'
    }
    catch {
        if ($_.Exception.Message -eq 'Validator accepted a publish missing its user-facing executable.') {
            throw
        }
    }

    Write-Host 'Release-layout fixture tests passed for all six desktop RIDs.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
