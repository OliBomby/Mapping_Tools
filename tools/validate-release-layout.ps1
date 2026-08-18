[CmdletBinding()]
param(
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryX86,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryX64,

    [Parameter(Mandatory = $true)]
    [string]$LegacyX86,

    [Parameter(Mandatory = $true)]
    [string]$LegacyX64,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryArchiveX86,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryArchiveX64,

    [Parameter(Mandatory = $true)]
    [string]$LegacyArchiveX86,

    [Parameter(Mandatory = $true)]
    [string]$LegacyArchiveX64,

    [string]$InstallerX86,

    [string]$InstallerX64
)

$primaryDirectories = @($PrimaryX86, $PrimaryX64)
$legacyDirectories = @($LegacyX86, $LegacyX64)
$archives = @(
    $PrimaryArchiveX86,
    $PrimaryArchiveX64,
    $LegacyArchiveX86,
    $LegacyArchiveX64
)

foreach ($directory in $primaryDirectories + $legacyDirectories) {
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Missing publish directory: $directory"
    }

    $executable = Join-Path $directory 'Mapping Tools.exe'
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw "Missing user-facing executable: $executable"
    }

    $isPrimary = $primaryDirectories -contains $directory
    $requiredPublishFiles = if ($isPrimary) {
        @(
            'Mapping_Tools.Desktop.dll',
            'Mapping_Tools.Desktop.deps.json',
            'Mapping_Tools.Desktop.runtimeconfig.json'
        )
    }
    else {
        @(
            'Mapping Tools.dll',
            'Mapping Tools.deps.json',
            'Mapping Tools.runtimeconfig.json'
        )
    }

    foreach ($file in $requiredPublishFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $directory $file) -PathType Leaf)) {
            throw "Publish directory '$directory' is missing '$file'."
        }
    }

    $wrongAssembly = if ($isPrimary) { 'Mapping Tools.dll' } else { 'Mapping_Tools.Desktop.dll' }
    if (Test-Path -LiteralPath (Join-Path $directory $wrongAssembly) -PathType Leaf) {
        throw "Publish directory '$directory' contains the other frontend assembly '$wrongAssembly'."
    }

    if ($isPrimary -and
        (Test-Path -LiteralPath (Join-Path $directory 'Mapping_Tools.Desktop.exe') -PathType Leaf)) {
        throw "Primary publish directory '$directory' was not renamed to the user-facing apphost."
    }

    if ($ExpectedVersion) {
        $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(
            (Resolve-Path -LiteralPath $executable)).FileVersion
        if ($fileVersion -ne $ExpectedVersion) {
            throw "Unexpected file version '$fileVersion' in $executable; expected '$ExpectedVersion'."
        }
    }
}

foreach ($archive in $archives) {
    if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) {
        throw "Missing release archive: $archive"
    }
}

if ([string]::IsNullOrWhiteSpace($InstallerX86) -xor
    [string]::IsNullOrWhiteSpace($InstallerX64)) {
    throw 'Both installer outputs must be supplied together.'
}

if (-not [string]::IsNullOrWhiteSpace($InstallerX86)) {
    foreach ($installer in @($InstallerX86, $InstallerX64)) {
        if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
            throw "Missing installer output: $installer"
        }
    }
}

if ($PrimaryX86 -notlike '*Mapping_Tools.Desktop*' -or
    $PrimaryX64 -notlike '*Mapping_Tools.Desktop*') {
    throw 'Primary release directories must come from Mapping_Tools.Desktop.'
}

if ($LegacyX86 -like '*Mapping_Tools.Desktop*' -or
    $LegacyX64 -like '*Mapping_Tools.Desktop*' -or
    $LegacyX86 -notlike '*Mapping_Tools*' -or
    $LegacyX64 -notlike '*Mapping_Tools*') {
    throw 'Fallback release directories must come from the legacy WPF project.'
}

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

function Assert-ArchiveLayout(
    [string]$archive,
    [bool]$isPrimary) {
    $entries = Get-ZipEntryNames $archive
    $entrySet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        [void]$entrySet.Add($entry.TrimStart('/'))
        if ($entry -match '[/\\]') {
            throw "Release archive '$archive' contains a non-root entry '$entry'."
        }
        if ($entry -match '\.zip$') {
            throw "Release archive '$archive' contains a nested ZIP archive '$entry'."
        }
    }

    $required = if ($isPrimary) {
        @(
            'Mapping Tools.exe',
            'Mapping_Tools.Desktop.dll',
            'Mapping_Tools.Desktop.deps.json',
            'Mapping_Tools.Desktop.runtimeconfig.json'
        )
    }
    else {
        @(
            'Mapping Tools.exe',
            'Mapping Tools.dll',
            'Mapping Tools.deps.json',
            'Mapping Tools.runtimeconfig.json'
        )
    }

    foreach ($entry in $required) {
        if (-not $entrySet.Contains($entry)) {
            throw "Release archive '$archive' is missing '$entry'."
        }
    }

    if ($isPrimary -and $entrySet.Contains('Mapping Tools.dll')) {
        throw "Primary Avalonia archive '$archive' contains the legacy WPF assembly."
    }
    if (-not $isPrimary -and $entrySet.Contains('Mapping_Tools.Desktop.dll')) {
        throw "Legacy WPF archive '$archive' contains the Avalonia assembly."
    }
    if ($entrySet.Contains('Mapping_Tools.Desktop.exe')) {
        throw "Release archive '$archive' contains the pre-rename Avalonia apphost."
    }
}

Assert-ArchiveLayout $PrimaryArchiveX86 $true
Assert-ArchiveLayout $PrimaryArchiveX64 $true
Assert-ArchiveLayout $LegacyArchiveX86 $false
Assert-ArchiveLayout $LegacyArchiveX64 $false
