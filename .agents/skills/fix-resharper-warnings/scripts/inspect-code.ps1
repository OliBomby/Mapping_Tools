[CmdletBinding()]
param(
    [switch]$FailOnWarnings
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$manifest = Get-Content -LiteralPath (Join-Path $repoRoot '.config/dotnet-tools.json') -Raw | ConvertFrom-Json
$toolVersion = $manifest.tools.'jetbrains.resharper.globaltools'.version
$packageRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
$toolPath = Join-Path $packageRoot "jetbrains.resharper.globaltools/$toolVersion/tools/net8.0/any/inspectcode.exe"

Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath $toolPath)) {
        & dotnet tool restore
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $toolPath)) {
            throw "Could not locate the pinned InspectCode $toolVersion package."
        }
    }

    # The same executable starts .NET Framework when launched without dotnet.
    $toolInfo = & dotnet $toolPath --version
    if ($LASTEXITCODE -ne 0) { throw 'Could not start InspectCode through dotnet.' }
    $runtime = [regex]::Match(($toolInfo -join "`n"), '\.NET (?<major>\d+)\.')
    if (-not $runtime.Success) { throw "Unexpected InspectCode runtime: $toolInfo" }

    $cacheHome = Join-Path $repoRoot "artifacts/resharper/cache/$toolVersion-dotnet$($runtime.Groups['major'].Value)"
    [IO.Directory]::CreateDirectory($cacheHome) | Out-Null
    try {
        $cacheLock = [IO.File]::Open((Join-Path $cacheHome 'analysis.lock'),
            [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    }
    catch [IO.IOException] {
        throw 'Another analysis is using this cache. Let it finish, then retry; do not create a fresh cache for a concurrent run.'
    }

    try {
        $runId = '{0}-{1}' -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), [Guid]::NewGuid().ToString('N').Substring(0, 8)
        $runDirectory = Join-Path $repoRoot "artifacts/resharper/$runId"
        [IO.Directory]::CreateDirectory($runDirectory) | Out-Null
        $reportPath = Join-Path $runDirectory 'warnings.sarif'
        $inspectArguments = @(
            'Mapping_Tools.slnx'
            "--output=$reportPath"
            '--severity=WARNING'
            '--swea'
            '--settings=Mapping_Tools.sln.DotSettings'
            '--disable-settings-layers=GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal'
            "--caches-home=$cacheHome"
            '--properties=BaseOutputPath=bin/agent/'
            '--build'
            '--no-updates'
            '--verbosity=WARN'
            '--LogLevel=VERBOSE'
            "--LogFolder=$(Join-Path $runDirectory 'logs')"
        )
        Write-Output "InspectCode $toolVersion / .NET $($runtime.Groups['major'].Value); cache: $cacheHome"
        $timer = [Diagnostics.Stopwatch]::StartNew()
        & dotnet $toolPath @inspectArguments
        $inspectExitCode = $LASTEXITCODE
        $timer.Stop()
        if ($inspectExitCode -ne 0) { throw "InspectCode failed with exit code $inspectExitCode. Logs: $runDirectory" }

        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        if ($report.runs[0].invocations[0].executionSuccessful -ne $true) {
            throw "InspectCode did not finish successfully. Logs: $runDirectory"
        }
        $findings = @($report.runs[0].results)
        Write-Output ('Completed in {0:N1}s; findings: {1}; report: {2}' -f
            $timer.Elapsed.TotalSeconds, $findings.Count, $reportPath)
        if ($FailOnWarnings -and $findings.Count -gt 0) {
            throw "InspectCode reported $($findings.Count) warning(s). Report: $reportPath"
        }
    }
    finally {
        $cacheLock.Dispose()
    }
}
finally {
    Pop-Location
}
