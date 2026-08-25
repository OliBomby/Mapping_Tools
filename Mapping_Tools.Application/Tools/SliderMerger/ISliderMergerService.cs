namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Runs Slider Merger through the shared live-aware editor gateway.</summary>
public interface ISliderMergerService
{
    /// <summary>Loads, transforms, backs up, and saves each requested beatmap.</summary>
    /// <param name="paths">Beatmap paths in the shell's selected order.</param>
    /// <param name="options">The import, connection, and geometry settings.</param>
    /// <param name="progress">Optional aggregate normalized progress reporting.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, or saving.</param>
    /// <returns>The processed paths and merged-object count.</returns>
    Task<SliderMergerResult> MergeAsync(
        IReadOnlyList<string> paths,
        SliderMergerProject options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
