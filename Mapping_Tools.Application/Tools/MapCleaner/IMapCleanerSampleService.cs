using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Application.Tools.MapCleaner;

/// <summary>Inspects mapset samples and moves unused files to recoverable storage.</summary>
public interface IMapCleanerSampleService
{
    /// <summary>Finds samples used by the mapset and optionally detects duplicate files.</summary>
    /// <param name="directory">The mapset directory to inspect.</param>
    /// <param name="detectDuplicates">Whether content-identical samples should be reported.</param>
    /// <param name="cancellationToken">Cancels file inspection.</param>
    /// <returns>A case-insensitive mapping of redundant sample names to retained names.</returns>
    Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        bool detectDuplicates,
        CancellationToken cancellationToken = default);

    /// <summary>Moves samples unused by maps and storyboards into a recovery directory.</summary>
    /// <param name="directory">The mapset directory to clean.</param>
    /// <param name="currentBeatmapPath">The active beatmap, which must remain available.</param>
    /// <param name="currentBeatmap">The active parsed beatmap.</param>
    /// <param name="cancellationToken">Cancels file inspection or movement.</param>
    /// <returns>The number of sample files moved.</returns>
    Task<int> MoveUnusedToRecoveryAsync(
        string directory,
        string currentBeatmapPath,
        Beatmap currentBeatmap,
        CancellationToken cancellationToken = default);
}

