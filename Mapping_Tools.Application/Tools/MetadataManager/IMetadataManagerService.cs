using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.Tools.MetadataManager;

/// <summary>
///     Imports metadata and applies it to one or more beatmaps through application ports.
/// </summary>
public interface IMetadataManagerService
{
    /// <summary>
    ///     Reads metadata from a disk beatmap without modifying it.
    /// </summary>
    /// <param name="path">The source beatmap path.</param>
    /// <param name="cancellationToken">Cancels before or during the read.</param>
    /// <returns>Editable metadata and colour values.</returns>
    Task<MetadataManagerEngineOptions> ImportAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Applies the configured state to every target and renames each output from
    ///     its resulting metadata-derived filename.
    /// </summary>
    /// <param name="options">The metadata values and vertical-bar-separated targets.</param>
    /// <param name="progress">Receives normalized completion after each target.</param>
    /// <param name="cancellationToken">Cancels before the next target or destructive write.</param>
    /// <returns>The paths written by the operation.</returns>
    Task<MetadataManagerResult> ExportAsync(
        MetadataManagerServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
