[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-x86', 'win-x64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Channel,

    [Parameter(Mandatory = $true)]
    [ValidateSet('win-x86', 'win-x64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$RuntimeIdentifier,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseDirectory,

    [switch]$RequireLegacyBridge
)

if ($Channel -cne $RuntimeIdentifier) {
    throw "The Velopack channel '$Channel' must equal the runtime identifier '$RuntimeIdentifier'."
}

if (-not (Test-Path -LiteralPath $ReleaseDirectory -PathType Container)) {
    throw "Missing Velopack release directory: $ReleaseDirectory"
}

function Get-ZipEntryNames([string]$archivePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $archivePath))
    try {
        return @($zip.Entries | ForEach-Object { $_.FullName.TrimStart('/') })
    }
    finally {
        $zip.Dispose()
    }
}

function Assert-File([string]$path, [string]$description) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing $description`: $path"
    }
}

$feedPath = Join-Path $ReleaseDirectory "releases.$Channel.json"
$assetsPath = Join-Path $ReleaseDirectory "assets.$Channel.json"
Assert-File $feedPath 'Velopack release feed'
Assert-File $assetsPath 'Velopack assets manifest'

try {
    $feed = Get-Content -LiteralPath $feedPath -Raw | ConvertFrom-Json
    $assetsManifest = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
}
catch {
    throw "Velopack metadata is not valid JSON: $($_.Exception.Message)"
}

$feedAssets = @($feed.Assets)
if ($feedAssets.Count -eq 0) {
    throw "Velopack feed '$feedPath' contains no assets."
}
$assetsManifestEntries = @($assetsManifest)
if ($assetsManifestEntries.Count -eq 0) {
    throw "Velopack assets manifest '$assetsPath' contains no assets."
}

$currentAssets = @($feedAssets | Where-Object { [string]$_.Version -eq $ExpectedVersion })
$currentFullAssets = @($currentAssets | Where-Object { [string]$_.Type -eq 'Full' })
if ($currentFullAssets.Count -ne 1) {
    throw "Velopack feed '$feedPath' must contain exactly one full asset for version '$ExpectedVersion'."
}

foreach ($asset in $feedAssets) {
    $fileName = [string]$asset.FileName
    if ([string]::IsNullOrWhiteSpace($fileName) -or
        [System.IO.Path]::IsPathRooted($fileName) -or
        $fileName.Replace('\', '/') -match '(^|/)\.\.(?:/|$)') {
        throw "Velopack feed '$feedPath' contains an unsafe asset path '$fileName'."
    }

    if ([string]$asset.Version -eq $ExpectedVersion) {
        Assert-File (Join-Path $ReleaseDirectory ([System.IO.Path]::GetFileName($fileName))) "current Velopack asset '$fileName'"
    }
}

$currentDeltaAssets = @($currentAssets | Where-Object { [string]$_.Type -eq 'Delta' })
if ($currentDeltaAssets.Count -gt 1) {
    throw "Velopack feed '$feedPath' contains more than one delta for version '$ExpectedVersion'."
}

$releaseFiles = @(Get-ChildItem -LiteralPath $ReleaseDirectory -File)
switch -Regex ($RuntimeIdentifier) {
    '^win-' {
        if (-not ($releaseFiles | Where-Object Name -like '*-Setup.exe')) {
            throw "Windows release '$Channel' did not produce a per-user Velopack Setup.exe."
        }
        if (-not ($releaseFiles | Where-Object Name -like '*-Portable.zip')) {
            throw "Windows release '$Channel' did not produce the Velopack portable package."
        }
    }
    '^linux-' {
        $appImage = $releaseFiles | Where-Object Name -like '*.AppImage' | Select-Object -First 1
        if ($null -eq $appImage) {
            throw "Linux release '$Channel' did not produce an AppImage."
        }
        if ($env:OS -ne 'Windows_NT') {
            $getUnixFileMode = [System.IO.File].GetMethod(
                'GetUnixFileMode',
                [Type[]]@([string]))
            if ($null -eq $getUnixFileMode) {
                throw 'This PowerShell runtime cannot inspect Unix executable permissions.'
            }

            $mode = [System.IO.File]::GetUnixFileMode($appImage.FullName)
            if (([int]$mode -band 0x40) -eq 0) {
                throw "Linux AppImage '$($appImage.Name)' is not executable."
            }
        }
    }
    '^osx-' {
        if (-not ($releaseFiles | Where-Object Name -like '*.pkg')) {
            throw "macOS release '$Channel' did not produce a pkg installer."
        }
        if (-not ($releaseFiles | Where-Object Name -like '*-Portable.zip')) {
            throw "macOS release '$Channel' did not produce a portable package."
        }
    }
}

if ($RequireLegacyBridge) {
    if ($RuntimeIdentifier -notlike 'win-*') {
        throw 'The legacy Program Files bridge is only valid for Windows releases.'
    }

    $bridgeName = if ($RuntimeIdentifier -eq 'win-x86') { 'release.zip' } else { 'release_x64.zip' }
    $bridgePath = Join-Path $ReleaseDirectory $bridgeName
    Assert-File $bridgePath 'legacy migration bridge'

    $bridgeEntries = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in Get-ZipEntryNames $bridgePath) {
        [void]$bridgeEntries.Add($entry)
    }

    if ($bridgeEntries.Count -ne 1 -or -not $bridgeEntries.Contains('Mapping Tools.exe')) {
        throw "Legacy migration bridge '$bridgePath' must contain only the Velopack installer named 'Mapping Tools.exe'."
    }
}

Write-Host "Validated Velopack channel '$Channel' for version '$ExpectedVersion'."
