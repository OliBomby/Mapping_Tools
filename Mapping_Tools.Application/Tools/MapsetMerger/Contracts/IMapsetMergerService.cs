using Mapping_Tools.Application.Tools.MapsetMerger.Models;

namespace Mapping_Tools.Application.Tools.MapsetMerger.Contracts;

/// <summary>
///     Runs Mapset Merger against disk-only source documents and an export transaction.
/// </summary>
public interface IMapsetMergerService
{
    /// <summary>
    ///     Reads all requested source mapsets, rewrites document references, stages
    ///     every output, and commits only after the complete export succeeds.
    /// </summary>
    /// <param name="project">The validated merge project.</param>
    /// <param name="progress">Optional aggregate normalized progress reporting.</param>
    /// <param name="cancellationToken">Cancels parsing, staging, or commit.</param>
    /// <param name="overwrite">Whether generated files may replace existing output files.</param>
    /// <param name="clearExisting">Whether to remove existing output files; requires overwrite permission.</param>
    /// <returns>Counts and final file paths for the committed output.</returns>
    Task<MapsetMergerResult> MergeAsync(
        MapsetMergerServiceOptions project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        bool overwrite = true,
        bool clearExisting = false);
}
