using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.MetadataManager;

/// <summary>
/// Represents the complete Metadata Manager project persisted by the shell.
/// </summary>
/// <remarks>
/// The direct property layout intentionally matches the legacy
/// <c>MetadataManagerVm</c> JSON document.
/// </remarks>
public sealed class MetadataManagerProject : MetadataManagerOptions
{
}

/// <summary>Reports the final paths written by one Metadata Manager run.</summary>
/// <param name="ProcessedPaths">The output paths in target selection order.</param>
public sealed record MetadataManagerResult(IReadOnlyList<string> ProcessedPaths)
{
    /// <summary>Gets the number of beatmaps successfully written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

/// <summary>
/// Imports metadata and applies it to one or more beatmaps through application ports.
/// </summary>
public interface IMetadataManagerService
{
    /// <summary>
    /// Reads metadata from a disk beatmap without modifying it.
    /// </summary>
    /// <param name="path">The source beatmap path.</param>
    /// <param name="cancellationToken">Cancels before or during the read.</param>
    /// <returns>Editable metadata and colour values.</returns>
    Task<MetadataManagerOptions> ImportAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the configured state to every target and renames each output from
    /// its resulting metadata-derived filename.
    /// </summary>
    /// <param name="options">The metadata values and vertical-bar-separated targets.</param>
    /// <param name="progress">Receives completion percentages after each target.</param>
    /// <param name="cancellationToken">Cancels before the next target or destructive write.</param>
    /// <returns>The paths written by the operation.</returns>
    Task<MetadataManagerResult> ExportAsync(
        MetadataManagerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
