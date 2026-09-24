$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reportDirectory = Join-Path $repository 'coverage-report'
$outputDirectory = Join-Path $reportDirectory 'mutation'

if (Test-Path $outputDirectory) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

Push-Location (Join-Path $repository 'Mapping_Tools.Core.Tests')
try {
    & dotnet stryker --output $outputDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Stryker failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$reports = @(Get-ChildItem -Path $outputDirectory -Filter 'mutation-report.md' -Recurse -File)
if ($reports.Count -ne 1) {
    throw "Expected one Stryker Markdown report, found $($reports.Count)."
}

$report = Get-Content $reports[0].FullName -Raw
$scoreLine = ($report -split '\r?\n' | Where-Object { $_ -match 'final mutation score' } | Select-Object -Last 1)
if (-not $scoreLine) {
    throw 'Stryker report did not contain a mutation score.'
}

$scoreLine = $scoreLine.TrimStart('#', ' ') -replace '(?<=\d),(?=\d)', '.'
$summary = "## Core mutation testing`n`n$scoreLine`n`nMutation testing currently covers Mapping_Tools.Core with Mapping_Tools.Core.Tests.`n"
Set-Content -Path (Join-Path $reportDirectory 'mutation-summary.md') -Value $summary
Write-Output $summary
