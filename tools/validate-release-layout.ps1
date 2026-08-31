[CmdletBinding()]
param(
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string[]]$PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string[]]$Archive,

    [Parameter(Mandatory = $true)]
    [string[]]$ExecutableName,

    [Parameter(Mandatory = $true)]
    [string[]]$AssetName,

    [string[]]$ArchiveExecutableName,

    [string]$CompatibilityArchiveX86,

    [string]$CompatibilityArchiveX64,

    [string]$InstallerX86,

    [string]$InstallerX64
)

if ($PublishDirectory.Count -ne $Archive.Count -or
    $PublishDirectory.Count -ne $ExecutableName.Count -or
    $PublishDirectory.Count -ne $AssetName.Count) {
    throw 'Publish directories, archives, executable names, and asset names must have matching counts.'
}

if ($ArchiveExecutableName.Count -ne 0 -and
    $ArchiveExecutableName.Count -ne $PublishDirectory.Count) {
    throw 'Archive executable names must be omitted or have the same count as the publish directories.'
}

if ($PublishDirectory.Count -eq 0) {
    throw 'At least one desktop publish must be supplied.'
}

$isWindowsHost = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

function Get-ZipEntryNames([string]$archive) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $archive))
    try {
        return @($zip.Entries | ForEach-Object FullName)
    }
    finally {
        $zip.Dispose()
    }
}

function Get-FileSha256([string]$path) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $fileStream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $path))
    try {
        return [System.BitConverter]::ToString($sha256.ComputeHash($fileStream)).Replace('-', '')
    }
    finally {
        $fileStream.Dispose()
        $sha256.Dispose()
    }
}

function Assert-ArchiveLayout(
    [string]$archivePath,
    [string]$expectedExecutableName) {
    $separatorIndex = $expectedExecutableName.LastIndexOf('/')
    $archivePrefix = if ($separatorIndex -ge 0) {
        $expectedExecutableName.Substring(0, $separatorIndex + 1)
    }
    else {
        ''
    }
    $archiveRoot = $archivePrefix.TrimEnd('/')
    $archiveScope = $archiveRoot
    if ($expectedExecutableName.EndsWith(
            '.app/Contents/MacOS/Mapping Tools',
            [StringComparison]::OrdinalIgnoreCase)) {
        $contentsMarker = '/Contents/'
        $contentsIndex = $expectedExecutableName.IndexOf(
            $contentsMarker,
            [StringComparison]::OrdinalIgnoreCase)
        $archiveScope = $expectedExecutableName.Substring(0, $contentsIndex)
    }
    $entries = Get-ZipEntryNames $archivePath
    $entrySet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($entry in $entries) {
        $normalizedEntry = $entry.TrimStart('/')
        [void]$entrySet.Add($normalizedEntry)
        if ($normalizedEntry -match '[/\\]' -and
            ([string]::IsNullOrWhiteSpace($archiveScope) -or
             -not $normalizedEntry.StartsWith("$archiveScope/", [StringComparison]::OrdinalIgnoreCase))) {
            throw "Release archive '$archivePath' contains a non-root entry '$entry'."
        }
        if ($normalizedEntry -match '\.zip$') {
            throw "Release archive '$archivePath' contains a nested ZIP archive '$normalizedEntry'."
        }
    }

    $required = @(
        $expectedExecutableName,
        ($archivePrefix + 'Mapping_Tools.Desktop.dll'),
        ($archivePrefix + 'Mapping_Tools.Desktop.deps.json'),
        ($archivePrefix + 'Mapping_Tools.Desktop.runtimeconfig.json')
    )

    if ($expectedExecutableName.EndsWith(
            '.app/Contents/MacOS/Mapping Tools',
            [StringComparison]::OrdinalIgnoreCase)) {
        $contentsMarker = '/Contents/MacOS/'
        $contentsIndex = $expectedExecutableName.IndexOf(
            $contentsMarker,
            [StringComparison]::OrdinalIgnoreCase)
        $contentsPrefix = $expectedExecutableName.Substring(
            0,
            $contentsIndex + '/Contents/'.Length)
        $required += $contentsPrefix + 'Info.plist'
    }

    foreach ($entry in $required) {
        if (-not $entrySet.Contains($entry)) {
            throw "Release archive '$archivePath' is missing '$entry'."
        }
    }

    if ($entrySet.Contains(($archivePrefix + 'Mapping Tools.dll'))) {
        throw "Release archive '$archivePath' contains the removed WPF assembly."
    }

    $preRenameAppHost = if ($expectedExecutableName.EndsWith('Mapping Tools.exe', [StringComparison]::OrdinalIgnoreCase)) {
        ($archivePrefix + 'Mapping_Tools.Desktop.exe')
    }
    else {
        ($archivePrefix + 'Mapping_Tools.Desktop')
    }

    if ($entrySet.Contains($preRenameAppHost)) {
        throw "Release archive '$archivePath' contains the pre-rename Avalonia apphost."
    }

    if ($expectedExecutableName.EndsWith('Mapping Tools', [StringComparison]::OrdinalIgnoreCase) -and -not $isWindowsHost) {
        $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $archivePath))
        try {
            $executableEntry = $zip.Entries |
                Where-Object FullName -eq $expectedExecutableName |
                Select-Object -First 1
            $unixMode = ([int64]$executableEntry.ExternalAttributes -shr 16) -band 0x1ff
            if (($unixMode -band 0x40) -eq 0) {
                throw "Release archive '$archivePath' does not preserve the executable mode for '$expectedExecutableName'."
            }
        }
        finally {
            $zip.Dispose()
        }
    }
}

for ($index = 0; $index -lt $PublishDirectory.Count; $index++) {
    $publishPath = $PublishDirectory[$index]
    $archivePath = $Archive[$index]
    $expectedExecutableName = $ExecutableName[$index]
    $expectedArchiveExecutableName = if ($ArchiveExecutableName.Count -eq 0) {
        $expectedExecutableName
    }
    else {
        $ArchiveExecutableName[$index]
    }
    $expectedAssetName = $AssetName[$index]

    if (-not (Test-Path -LiteralPath $publishPath -PathType Container)) {
        throw "Missing publish directory: $publishPath"
    }

    if ($publishPath -notlike '*Mapping_Tools.Desktop*') {
        throw "Primary release directory must come from Mapping_Tools.Desktop: $publishPath"
    }

    if ([System.IO.Path]::GetFileName($archivePath) -cne $expectedAssetName) {
        throw "Archive '$archivePath' does not have the deterministic asset name '$expectedAssetName'."
    }

    $executablePath = Join-Path $publishPath $expectedExecutableName
    if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        throw "Missing user-facing executable: $executablePath"
    }

    if ($expectedExecutableName -eq 'Mapping Tools' -and -not $isWindowsHost) {
        $unixMode = [System.IO.File]::GetUnixFileMode((Resolve-Path -LiteralPath $executablePath))
        if (([int]$unixMode -band 0x40) -eq 0) {
            throw "Unix publish apphost '$executablePath' is not executable."
        }
    }

    $requiredPublishFiles = @(
        'Mapping_Tools.Desktop.dll',
        'Mapping_Tools.Desktop.deps.json',
        'Mapping_Tools.Desktop.runtimeconfig.json'
    )

    foreach ($file in $requiredPublishFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $publishPath $file) -PathType Leaf)) {
            throw "Publish directory '$publishPath' is missing '$file'."
        }
    }

    if (Test-Path -LiteralPath (Join-Path $publishPath 'Mapping Tools.dll') -PathType Leaf) {
        throw "Publish directory '$publishPath' contains a removed WPF assembly."
    }

    $preRenameAppHost = if ($expectedExecutableName -eq 'Mapping Tools.exe') {
        'Mapping_Tools.Desktop.exe'
    }
    else {
        'Mapping_Tools.Desktop'
    }

    if (Test-Path -LiteralPath (Join-Path $publishPath $preRenameAppHost) -PathType Leaf) {
        throw "Publish directory '$publishPath' was not renamed to '$expectedExecutableName'."
    }

    if ($ExpectedVersion) {
        $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(
            (Resolve-Path -LiteralPath (Join-Path $publishPath 'Mapping_Tools.Desktop.dll'))).FileVersion
        if ($fileVersion -ne $ExpectedVersion) {
            throw "Unexpected file version '$fileVersion' in $publishPath; expected '$ExpectedVersion'."
        }
    }

    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        throw "Missing release archive: $archivePath"
    }

    Assert-ArchiveLayout $archivePath $expectedArchiveExecutableName
}

if ([string]::IsNullOrWhiteSpace($CompatibilityArchiveX86) -xor
    [string]::IsNullOrWhiteSpace($CompatibilityArchiveX64)) {
    throw 'Both Windows compatibility archives must be supplied together.'
}

if (-not [string]::IsNullOrWhiteSpace($CompatibilityArchiveX86)) {
    $compatibilityArchives = @(
        $CompatibilityArchiveX86,
        $CompatibilityArchiveX64
    )
    $canonicalWindowsArchives = @($Archive[0], $Archive[1])
    $compatibilityNames = @('release.zip', 'release_x64.zip')

    for ($index = 0; $index -lt $compatibilityArchives.Count; $index++) {
        $compatibilityArchive = $compatibilityArchives[$index]
        if ([System.IO.Path]::GetFileName($compatibilityArchive) -cne $compatibilityNames[$index]) {
            throw "Windows compatibility archive '$compatibilityArchive' has an unexpected name."
        }
        if (-not (Test-Path -LiteralPath $compatibilityArchive -PathType Leaf)) {
            throw "Missing Windows compatibility archive: $compatibilityArchive"
        }

        $canonicalHash = Get-FileSha256 $canonicalWindowsArchives[$index]
        $compatibilityHash = Get-FileSha256 $compatibilityArchive
        if ($canonicalHash -cne $compatibilityHash) {
            throw "Windows compatibility archive '$compatibilityArchive' is not an exact canonical-asset copy."
        }
    }
}

if ([string]::IsNullOrWhiteSpace($InstallerX86) -xor
    [string]::IsNullOrWhiteSpace($InstallerX64)) {
    throw 'Both Windows installer outputs must be supplied together.'
}

if (-not [string]::IsNullOrWhiteSpace($InstallerX86)) {
    $installers = @($InstallerX86, $InstallerX64)
    $installerNames = @('mapping_tools_installer_x86.exe', 'mapping_tools_installer_x64.exe')
    for ($index = 0; $index -lt $installers.Count; $index++) {
        $installer = $installers[$index]
        if ([System.IO.Path]::GetFileName($installer) -cne $installerNames[$index]) {
            throw "Windows installer '$installer' has an unexpected name."
        }
        if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
            throw "Missing Windows installer output: $installer"
        }
    }
}
