[CmdletBinding(DefaultParameterSetName = 'Render')]
param(
    [Parameter(Mandatory)] [ValidateSet('avalonia', 'wpf')] [string] $Framework,
    [Parameter(ParameterSetName = 'Render')] [string] $View,
    [Parameter(ParameterSetName = 'Render')] [string] $Output,
    [Parameter(ParameterSetName = 'Render')] [ValidateRange(1, 10000)] [int] $Width = 1280,
    [Parameter(ParameterSetName = 'Render')] [ValidateRange(1, 10000)] [int] $Height = 800,
    [Parameter(Mandatory, ParameterSetName = 'List')] [switch] $List
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$projectName = if ($Framework -eq 'avalonia') { 'Mapping_Tools.Avalonia.ViewRenderer' } else { 'Mapping_Tools.Wpf.ViewRenderer' }
$project = Join-Path $repoRoot "tools\$projectName\$projectName.csproj"
$rendererArgs = @()
if ($List) {
    $rendererArgs += '--list'
} else {
    if ([string]::IsNullOrWhiteSpace($View)) { throw '-View is required when rendering.' }
    if ([string]::IsNullOrWhiteSpace($Output)) {
        $Output = Join-Path $repoRoot "artifacts\view-renders\$Framework-$View.png"
    } elseif (-not [System.IO.Path]::IsPathRooted($Output)) {
        $Output = Join-Path $repoRoot $Output
    }
    $Output = [System.IO.Path]::GetFullPath($Output)
    $rendererArgs += @('--view', $View, '--output', $Output, '--width', $Width, '--height', $Height)
}

$isolatedOutput = "artifacts\view-renderer\$Framework\"
dotnet run --project $project "-p:BaseOutputPath=$isolatedOutput" -- @rendererArgs
if ($LASTEXITCODE -ne 0) { throw "$projectName failed with exit code $LASTEXITCODE." }
if (-not $List) {
    if (-not (Test-Path -LiteralPath $Output)) { throw "Renderer completed without creating $Output." }
    Write-Output $Output
}
