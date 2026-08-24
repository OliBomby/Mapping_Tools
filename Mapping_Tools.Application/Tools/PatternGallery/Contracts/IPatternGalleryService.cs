using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery.Models;

namespace Mapping_Tools.Application.Tools.PatternGallery.Contracts;

/// <summary>Loads pattern data, imports patterns, restores collections, and places patterns.</summary>
public interface IPatternGalleryService
{
    /// <summary>Loads a stored pattern beatmap for presentation.</summary>
    /// <param name="pattern">The indexed pattern to load.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels the beatmap read.</param>
    /// <returns>The loaded beatmap with stacking information updated.</returns>
    Task<Beatmap> LoadBeatmapAsync(
        PatternGalleryPattern pattern,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports raw object and timing-point text as a new pattern file.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="hitObjectText">Newline-separated osu! hit-object lines.</param>
    /// <param name="timingPointText">Newline-separated osu! timing-point lines.</param>
    /// <param name="globalSv">The source global slider multiplier.</param>
    /// <param name="gameMode">The source game mode.</param>
    /// <param name="project">The collection receiving the pattern.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels parsing or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportCodeAsync(
        string name,
        string hitObjectText,
        string timingPointText,
        double globalSv,
        GameMode gameMode,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports a beatmap file, retaining only the requested objects when configured.</summary>
    /// <param name="sourcePath">The source `.osu` file.</param>
    /// <param name="name">The display name.</param>
    /// <param name="filter">An optional legacy time-code query.</param>
    /// <param name="startTime">An optional lower time bound.</param>
    /// <param name="endTime">An optional upper time bound.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels reading or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportFileAsync(
        string sourcePath,
        string name,
        string? filter,
        double startTime,
        double endTime,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports the hit objects selected in the current live editor state.</summary>
    /// <param name="sourcePath">The beatmap expected to be open in osu!.</param>
    /// <param name="name">The display name.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels live reading or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportSelectedAsync(
        string sourcePath,
        string name,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Places selected patterns into one target beatmap and saves it safely.</summary>
    /// <param name="targetPath">The beatmap to edit.</param>
    /// <param name="patterns">Selected pattern metadata.</param>
    /// <param name="project">The complete placement option snapshot.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="quick">Whether editor reload is requested after saving.</param>
    /// <param name="progress">Receives zero-to-one-hundred progress.</param>
    /// <param name="cancellationToken">Cancels before or between placements.</param>
    /// <returns>The successful placement count and legacy completion message.</returns>
    Task<PatternGalleryRunResult> ExportAsync(
        string targetPath,
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        bool quick,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes selected metadata and their physical pattern files.</summary>
    /// <param name="patterns">Patterns to remove.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels before deletion.</param>
    Task DeleteAsync(
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Reconciles indexed metadata with the physical Pattern Files directory.</summary>
    /// <param name="project">The collection to modify.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels between pattern reads.</param>
    /// <returns>The number of removed and newly indexed files.</returns>
    Task<PatternGalleryRestoreResult> RestoreAsync(
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);
}
