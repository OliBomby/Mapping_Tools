using Mapping_Tools.Core.Tools.SliderMerger;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Persists the complete Slider Merger form using the legacy property names.</summary>
public sealed class SliderMergerProject : SliderMergerOptions
{
}

/// <summary>Reports processed maps and source objects incorporated into merged sliders.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="ObjectsMerged">The number of source objects incorporated into merged sliders.</param>
public sealed record SliderMergerResult(
    IReadOnlyList<string> ProcessedPaths,
    int ObjectsMerged);

/// <summary>Runs Slider Merger through the shared live-aware editor gateway.</summary>
public interface ISliderMergerService
{
    /// <summary>Loads, transforms, backs up, and saves each requested beatmap.</summary>
    /// <param name="paths">Beatmap paths in the shell's selected order.</param>
    /// <param name="options">The import, connection, and geometry settings.</param>
    /// <param name="progress">Optional aggregate percentage reporting.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, or saving.</param>
    /// <returns>The processed paths and merged-object count.</returns>
    Task<SliderMergerResult> MergeAsync(
        IReadOnlyList<string> paths,
        SliderMergerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
