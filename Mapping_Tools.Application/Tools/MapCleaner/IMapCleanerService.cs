using Mapping_Tools.Core.Tools.MapCleaner.Models;

namespace Mapping_Tools.Application.Tools.MapCleaner;

/// <summary>Runs framework-independent Map Cleaner operations over selected beatmaps.</summary>
public interface IMapCleanerService
{
    /// <summary>Cleans all selected beatmaps and combines their change summaries.</summary>
    /// <param name="paths">The beatmap files to clean.</param>
    /// <param name="options">The cleanup and resnapping choices.</param>
    /// <param name="progress">Optional aggregate normalized completion reporting.</param>
    /// <param name="cancellationToken">Cancels loading, cleanup, or saving.</param>
    /// <returns>The combined cleanup counts and timeline markers.</returns>
    Task<MapCleanerResult> CleanAsync(
        IReadOnlyList<string> paths,
        MapCleanerProject.MapCleanerProjectOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
