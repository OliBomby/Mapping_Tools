namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>Runs Slider Completionator through the shared editor gateway.</summary>
public interface ISliderCompletionatorService
{
    /// <summary>
    ///     Applies the configured edits to each path and saves through the gateway.
    /// </summary>
    /// <param name="paths">Beatmap paths in the shell's selected order.</param>
    /// <param name="options">The import and slider-edit settings.</param>
    /// <param name="progress">Optional normalized progress receiver.</param>
    /// <param name="cancellationToken">Cancels discovery, transformation, or persistence.</param>
    /// <returns>A result containing processed paths and the total slider count.</returns>
    Task<SliderCompletionatorResult> CompleteAsync(
        IReadOnlyList<string> paths,
        SliderCompletionatorProject options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
