param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reportDirectory = Join-Path $repository 'coverage-report'
$rawDirectory = Join-Path $reportDirectory 'raw'
$mergedDirectory = Join-Path $reportDirectory 'merged'

if (Test-Path $rawDirectory) {
    Remove-Item -LiteralPath $rawDirectory -Recurse -Force
}

if (Test-Path $mergedDirectory) {
    Remove-Item -LiteralPath $mergedDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $rawDirectory -Force | Out-Null

foreach ($project in @('Core', 'Application', 'Infrastructure', 'Desktop')) {
    $testProject = Join-Path $repository "Mapping_Tools.$project.Tests/Mapping_Tools.$project.Tests.csproj"
    $testArguments = @(
        'test', $testProject,
        '--configuration', 'Release',
        '--no-restore',
        '--collect', 'XPlat Code Coverage',
        '--settings', (Join-Path $repository 'coverage.runsettings'),
        '--results-directory', (Join-Path $rawDirectory $project)
    )

    if ($NoBuild) {
        $testArguments += '--no-build'
    }

    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$project tests failed with exit code $LASTEXITCODE."
    }
}

$reports = @(Get-ChildItem -Path $rawDirectory -Filter 'coverage.cobertura.xml' -Recurse -File)
if ($reports.Count -ne 4) {
    throw "Expected coverage from four test projects, found $($reports.Count)."
}

$reportPaths = ($reports.FullName -join ';')
& dotnet reportgenerator "-reports:$reportPaths" "-targetdir:$mergedDirectory" '-reporttypes:Cobertura;Html;JsonSummary' '-assemblyfilters:+Mapping_Tools.Core;+Mapping_Tools.Application;+Mapping_Tools.Infrastructure;+Mapping_Tools.Desktop'
if ($LASTEXITCODE -ne 0) {
    throw "ReportGenerator failed with exit code $LASTEXITCODE."
}

$coverage = Get-Content (Join-Path $mergedDirectory 'Summary.json') -Raw | ConvertFrom-Json
$assemblies = @{}
foreach ($assembly in $coverage.coverage.assemblies) {
    $assemblies[$assembly.name] = $assembly
}

function Format-Coverage($covered, $total) {
    if ($total -eq 0) {
        return 'N/A'
    }

    return (100 * $covered / $total).ToString('F2', [Globalization.CultureInfo]::InvariantCulture) + '%'
}

$summaryLines = @(
    '## Test coverage',
    '',
    '| Project | Lines | Branches |',
    '| --- | ---: | ---: |'
)

foreach ($project in @('Core', 'Application', 'Infrastructure', 'Desktop')) {
    $name = "Mapping_Tools.$project"
    if (-not $assemblies.ContainsKey($name)) {
        throw "Merged coverage report is missing $name."
    }

    $assembly = $assemblies[$name]
    $linePercent = Format-Coverage $assembly.coveredlines $assembly.coverablelines
    $branchPercent = Format-Coverage $assembly.coveredbranches $assembly.totalbranches
    $summaryLines += "| $project | $linePercent | $branchPercent |"
}

$overallLines = Format-Coverage $coverage.summary.coveredlines $coverage.summary.coverablelines
$overallBranches = Format-Coverage $coverage.summary.coveredbranches $coverage.summary.totalbranches
$summaryLines += "| **Overall** | **$overallLines** | **$overallBranches** |"
$summaryLines += ''
$summaryLines += 'Each project row measures its production assembly across all four test projects.'
$summary = $summaryLines -join [Environment]::NewLine

Set-Content -Path (Join-Path $reportDirectory 'coverage-summary.md') -Value $summary
Write-Output $summary
