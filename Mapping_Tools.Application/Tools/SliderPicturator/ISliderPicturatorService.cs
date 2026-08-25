using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Runs Slider Picturator through the shared editor gateway and image port.</summary>
public interface ISliderPicturatorService
{
    /// <summary>Loads, transforms, backs up, saves, and optionally reloads one beatmap.</summary>
    /// <param name="path">The selected beatmap path.</param>
    /// <param name="options">The persisted settings and transient selected slider.</param>
    /// <param name="progress">Optional normalized completion reporting.</param>
    /// <param name="cancellationToken">Cancels loading, generation, or saving.</param>
    /// <returns>The written path and generated segment estimate.</returns>
    Task<SliderPicturatorResult> PicturateAsync(
        string path,
        SliderPicturatorServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the map palette and slider-track override for a beatmap.</summary>
    /// <param name="path">The beatmap path to inspect.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Palette colours in beatmap order.</returns>
    Task<IReadOnlyList<RgbaColour>> GetAvailableColorsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns the first selected slider from the current live editor state, if any.</summary>
    /// <param name="path">The beatmap path.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A selected slider, or <see langword="null" /> when none is selected.</returns>
    Task<HitObject?> GetSelectedSliderAsync(string path, CancellationToken cancellationToken = default);
}
