using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Application.MapCleaner;

public sealed class MapCleanerProject
{
    public MapCleanerOptions MapCleanerArgs { get; set; } = new();
}

public interface IMapCleanerSampleService
{
    Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        bool detectDuplicates,
        CancellationToken cancellationToken = default);

    Task<int> MoveUnusedToRecoveryAsync(
        string directory,
        string currentBeatmapPath,
        Beatmap currentBeatmap,
        CancellationToken cancellationToken = default);
}

public interface IMapCleanerService
{
    Task<MapCleanerResult> CleanAsync(
        IReadOnlyList<string> paths,
        MapCleanerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
