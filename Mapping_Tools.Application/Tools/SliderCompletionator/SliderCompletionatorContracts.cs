using Mapping_Tools.Core.Tools.SliderCompletionator;

namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>
///     Persists the complete Slider Completionator form while retaining the legacy
///     project property names used by WPF JSON files.
/// </summary>
public sealed class SliderCompletionatorProject : SliderCompletionatorOptions
{
}

/// <summary>Reports Slider Completionator's completed maps and slider count.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="SlidersCompleted">The number of selected sliders changed.</param>
public sealed record SliderCompletionatorResult(
    IReadOnlyList<string> ProcessedPaths,
    int SlidersCompleted);

/// <summary>Runs Slider Completionator through the shared editor gateway.</summary>
public interface ISliderCompletionatorService
{
    /// <summary>
    ///     Applies the configured edits to each path and saves through the gateway.
    /// </summary>
    /// <param name="paths">Beatmap paths in the shell's selected order.</param>
    /// <param name="options">The import and slider-edit settings.</param>
    /// <param name="progress">Optional progress receiver, reported as a percentage.</param>
    /// <param name="cancellationToken">Cancels discovery, transformation, or persistence.</param>
    /// <returns>A result containing processed paths and the total slider count.</returns>
    Task<SliderCompletionatorResult> CompleteAsync(
        IReadOnlyList<string> paths,
        SliderCompletionatorOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
