[CmdletBinding()]
param(
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryX86,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryX64,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryArchiveX86,

    [Parameter(Mandatory = $true)]
    [string]$PrimaryArchiveX64,

    [string]$InstallerX86,

    [string]$InstallerX64
)

$primaryDirectories = @($PrimaryX86, $PrimaryX64)
$archives = @($PrimaryArchiveX86, $PrimaryArchiveX64)

foreach ($directory in $primaryDirectories) {
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Missing publish directory: $directory"
    }

    $executable = Join-Path $directory 'Mapping Tools.exe'
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw "Missing user-facing executable: $executable"
    }

    $requiredPublishFiles = @(
        'Mapping_Tools.Desktop.dll',
        'Mapping_Tools.Desktop.deps.json',
        'Mapping_Tools.Desktop.runtimeconfig.json'
    )

    foreach ($file in $requiredPublishFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $directory $file) -PathType Leaf)) {
            throw "Publish directory '$directory' is missing '$file'."
        }
    }

    if (Test-Path -LiteralPath (Join-Path $directory 'Mapping Tools.dll') -PathType Leaf) {
        throw "Publish directory '$directory' contains a removed WPF assembly."
    }

    if (Test-Path -LiteralPath (Join-Path $directory 'Mapping_Tools.Desktop.exe') -PathType Leaf) {
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

function Assert-ArchiveLayout([string]$archive) {
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

    $required = @(
        'Mapping Tools.exe',
        'Mapping_Tools.Desktop.dll',
        'Mapping_Tools.Desktop.deps.json',
        'Mapping_Tools.Desktop.runtimeconfig.json'
    )

    foreach ($entry in $required) {
        if (-not $entrySet.Contains($entry)) {
            throw "Release archive '$archive' is missing '$entry'."
        }
    }

    if ($entrySet.Contains('Mapping Tools.dll')) {
        throw "Release archive '$archive' contains the removed WPF assembly."
    }
    if ($entrySet.Contains('Mapping_Tools.Desktop.exe')) {
        throw "Release archive '$archive' contains the pre-rename Avalonia apphost."
    }
}

Assert-ArchiveLayout $PrimaryArchiveX86
Assert-ArchiveLayout $PrimaryArchiveX64
