[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path $repositoryRoot 'tests\fixtures\wave0'
$manifestPath = Join-Path $fixtureRoot 'manifest.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

foreach ($fixture in $manifest.fixtures) {
    $path = [System.IO.Path]::GetFullPath((Join-Path $fixtureRoot $fixture.path))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Manifest fixture does not exist: $path"
    }

    $fixture.sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}

$json = $manifest | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($manifestPath, "$json`n", [System.Text.UTF8Encoding]::new($false))
Write-Host "Updated hashes in $manifestPath"
