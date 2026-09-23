[CmdletBinding()]
param()

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "mapping-tools-release-layout-$([Guid]::NewGuid())"
$validator = Join-Path $PSScriptRoot 'validate-release-layout.ps1'
$version = '1.2.3'
$rids = @('win-x86', 'win-x64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')

function New-FixtureFile([string]$path) {
    Set-Content -LiteralPath $path -Value 'fixture' -NoNewline
}

try {
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

    foreach ($rid in $rids) {
        $directory = Join-Path $testRoot "velopack-$rid"
        New-Item -ItemType Directory -Path $directory -Force | Out-Null

        $full = "MappingTools-$version-$rid-full.nupkg"
        $delta = "MappingTools-$version-$rid-delta.nupkg"
        New-FixtureFile (Join-Path $directory $full)
        New-FixtureFile (Join-Path $directory $delta)

        switch -Regex ($rid) {
            '^win-' {
                New-FixtureFile (Join-Path $directory "MappingTools-$rid-Setup.exe")
                New-FixtureFile (Join-Path $directory "MappingTools-$rid-Portable.zip")
            }
            '^linux-' {
                $appImage = Join-Path $directory "MappingTools-$rid.AppImage"
                New-FixtureFile $appImage
                if (-not $IsWindows) {
                    & chmod +x -- $appImage
                    if ($LASTEXITCODE -ne 0) {
                        throw "Could not mark Linux AppImage fixture executable: $appImage"
                    }
                }
            }
            '^osx-' {
                New-FixtureFile (Join-Path $directory "MappingTools-$rid-Setup.pkg")
                New-FixtureFile (Join-Path $directory "MappingTools-$rid-Portable.zip")
            }
        }

        $feed = @{
            Assets = @(
                @{
                    PackageId = "MappingTools-$rid"
                    Version = $version
                    Type = 'Full'
                    FileName = $full
                    SHA1 = 'fixture'
                    Size = 7
                },
                @{
                    PackageId = "MappingTools-$rid"
                    Version = $version
                    Type = 'Delta'
                    FileName = $delta
                    SHA1 = 'fixture'
                    Size = 7
                }
            )
        }
        $feed | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $directory "releases.$rid.json")
        @(
            @{ RelativeFileName = $full; Type = 'Full' },
            @{ RelativeFileName = $delta; Type = 'Delta' }
        ) |
            ConvertTo-Json -Depth 5 |
            Set-Content -LiteralPath (Join-Path $directory "assets.$rid.json")
        New-FixtureFile (Join-Path $directory "RELEASES-$rid")

        if ($rid -eq 'win-x86' -or $rid -eq 'win-x64') {
            $bridgeDirectory = Join-Path $testRoot "bridge-$rid"
            New-Item -ItemType Directory -Path $bridgeDirectory -Force | Out-Null
            New-FixtureFile (Join-Path $bridgeDirectory 'Mapping Tools.exe')
            $bridgeName = if ($rid -eq 'win-x86') { 'release.zip' } else { 'release_x64.zip' }
            Compress-Archive -Path (Join-Path $bridgeDirectory '*') `
                -DestinationPath (Join-Path $directory $bridgeName) -Force
        }

        & $validator `
            -Channel $rid `
            -RuntimeIdentifier $rid `
            -ExpectedVersion $version `
            -ReleaseDirectory $directory `
            -RequireLegacyBridge:($rid -like 'win-*')
    }

    $invalidDirectory = Join-Path $testRoot 'velopack-invalid'
    Copy-Item -LiteralPath (Join-Path $testRoot 'velopack-linux-x64') -Destination $invalidDirectory -Recurse
    Remove-Item -LiteralPath (Join-Path $invalidDirectory "MappingTools-$version-linux-x64-full.nupkg")
    try {
        & $validator `
            -Channel 'linux-x64' `
            -RuntimeIdentifier 'linux-x64' `
            -ExpectedVersion $version `
            -ReleaseDirectory $invalidDirectory
        throw 'Validator accepted a feed whose current full package is missing.'
    }
    catch {
        if ($_.Exception.Message -eq 'Validator accepted a feed whose current full package is missing.') {
            throw
        }
    }

    Write-Host 'Velopack release-layout fixture tests passed for all six channels.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
