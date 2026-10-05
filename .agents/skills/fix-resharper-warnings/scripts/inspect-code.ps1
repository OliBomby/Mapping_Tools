[CmdletBinding()]
param(
    [switch]$FailOnWarnings
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$toolManifest = Join-Path $repoRoot '.config/resharper/dotnet-tools.json'
$manifest = Get-Content -LiteralPath $toolManifest -Raw | ConvertFrom-Json
$toolVersion = $manifest.tools.'jetbrains.resharper.globaltools'.version
$isWindows = $env:OS -eq 'Windows_NT'
$userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
$packageRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $userProfile '.nuget/packages' }
$toolPath = Join-Path $packageRoot "jetbrains.resharper.globaltools/$toolVersion/tools/net8.0/any/inspectcode.exe"

Push-Location $repoRoot
try {
    if (-not $isWindows -or -not (Test-Path -LiteralPath $toolPath)) {
        & dotnet tool restore --tool-manifest $toolManifest
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $toolPath)) {
            throw "Could not locate the pinned InspectCode $toolVersion package."
        }
    }

    # The same executable starts .NET Framework when launched without dotnet.
    $toolInfo = if ($isWindows) {
        & dotnet $toolPath --version
    }
    else {
        & dotnet jb inspectcode --version
    }
    if ($LASTEXITCODE -ne 0) { throw 'Could not start InspectCode through dotnet.' }
    $runtime = [regex]::Match(($toolInfo -join "`n"), '\.NET (?<major>\d+)\.')
    if (-not $runtime.Success) { throw "Unexpected InspectCode runtime: $toolInfo" }

    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $sdkVersion) { throw 'Could not determine the active .NET SDK version.' }

    $cacheHome = Join-Path $repoRoot "artifacts/resharper/cache/$toolVersion-dotnet$($runtime.Groups['major'].Value)-sdk$sdkVersion"
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
        $logDirectory = Join-Path $runDirectory 'logs'
        [IO.Directory]::CreateDirectory($logDirectory) | Out-Null
        $timer = [Diagnostics.Stopwatch]::StartNew()

        $buildLogPath = Join-Path $logDirectory 'build.log'
        # Match InspectCode's .NET 10 Windows build host so it does not regenerate resource designers differently.
        $msbuildPath = $null
        if ($isWindows) {
            $vswherePath = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
            if (Test-Path -LiteralPath $vswherePath) {
                $msbuildPath = & $vswherePath -latest -products '*' -version '[18.0,)' -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\amd64\MSBuild.exe' |
                    Select-Object -First 1
            }
        }

        if ($msbuildPath -and (Test-Path -LiteralPath $msbuildPath)) {
            $buildOutput = & $msbuildPath Mapping_Tools.slnx /restore /t:Build '/property:Configuration=Debug' '/property:BaseOutputPath=bin/agent/' /verbosity:minimal /nologo 2>&1
            $buildCommand = "Prebuild with Visual Studio MSBuild: $msbuildPath"
        }
        else {
            $buildOutput = & dotnet build Mapping_Tools.slnx --configuration Debug '--property:BaseOutputPath=bin/agent/' --verbosity:minimal 2>&1
            $buildCommand = 'Prebuild with dotnet MSBuild'
        }

        $buildExitCode = $LASTEXITCODE
        @($buildCommand) + @($buildOutput) | Set-Content -LiteralPath $buildLogPath
        Write-Output $buildCommand
        $buildOutput | Write-Output
        if ($buildExitCode -ne 0) { throw "Solution build failed with exit code $buildExitCode. Log: $buildLogPath" }

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
            "--LogFolder=$logDirectory"
        )
        Write-Output "InspectCode $toolVersion / .NET $($runtime.Groups['major'].Value), SDK $sdkVersion; cache: $cacheHome"
        if ($isWindows) {
            & dotnet $toolPath @inspectArguments
        }
        else {
            & dotnet jb inspectcode @inspectArguments
        }
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
            throw "InspectCode reported $($findings.Count) finding(s). Report: $reportPath"
        }
    }
    finally {
        $cacheLock.Dispose()
    }
}
finally {
    Pop-Location
}
