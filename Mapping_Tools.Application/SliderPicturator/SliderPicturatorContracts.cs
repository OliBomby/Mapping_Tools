using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.Images;
using Mapping_Tools.Core.Tools.SliderPicturator;

namespace Mapping_Tools.Application.SliderPicturator;

/// <summary>Represents the complete persisted Slider Picturator project.</summary>
public sealed class SliderPicturatorProject : SliderPicturatorOptions
{
}

/// <summary>Reports the generated slider and the map written by Slider Picturator.</summary>
/// <param name="Path">The beatmap path written by the operation.</param>
/// <param name="SegmentCount">The estimated slider segment count.</param>
public sealed record SliderPicturatorResult(string Path, long SegmentCount);

/// <summary>Provides decoded source images without leaking bitmap-library types into the application.</summary>
public interface IImageFileService
{
    /// <summary>Loads a local image file into a framework-neutral pixel buffer.</summary>
    /// <param name="path">The local image path.</param>
    /// <param name="cancellationToken">Cancels before or during decoding.</param>
    /// <returns>The decoded RGBA image.</returns>
    Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>Runs Slider Picturator through the shared editor gateway and image port.</summary>
public interface ISliderPicturatorService
{
    /// <summary>Loads, transforms, backs up, saves, and optionally reloads one beatmap.</summary>
    /// <param name="path">The selected beatmap path.</param>
    /// <param name="options">The persisted settings and transient selected slider.</param>
    /// <param name="progress">Optional completion percentage reporting.</param>
    /// <param name="cancellationToken">Cancels loading, generation, or saving.</param>
    /// <returns>The written path and generated segment estimate.</returns>
    Task<SliderPicturatorResult> PicturateAsync(
        string path,
        SliderPicturatorOptions options,
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
