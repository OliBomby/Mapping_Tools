using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Coordinates Tumour Generator 2 imports and destructive runs.</summary>
public interface ITumourGeneratorService
{
    /// <summary>Imports sliders through the shared disk/live beatmap gateway.</summary>
    /// <param name="path">The beatmap path to inspect.</param>
    /// <param name="mode">The import source.</param>
    /// <param name="timeCode">The time query when <paramref name="mode" /> is time-based.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The imported sliders and map difficulty value.</returns>
    Task<TumourImportResult> ImportAsync(
        string path,
        HitObjectSelectionMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default);

    /// <summary>Runs, backs up, saves, and optionally reloads the requested maps.</summary>
    /// <param name="paths">The beatmaps to transform.</param>
    /// <param name="project">The complete settings snapshot.</param>
    /// <param name="reloadEditor">Whether a live source editor should be reloaded.</param>
    /// <param name="progress">Optional normalized progress receiver.</param>
    /// <param name="cancellationToken">Cancels between objects and save stages.</param>
    /// <returns>The transformed paths and slider count.</returns>
    Task<TumourRunResult> RunAsync(
        IReadOnlyList<string> paths,
        TumourGeneratorServiceOptions project,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
