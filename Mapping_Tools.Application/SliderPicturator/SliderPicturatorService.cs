using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.Images;
using Mapping_Tools.Core.Tools.SliderPicturator;

namespace Mapping_Tools.Application.SliderPicturator;

/// <summary>Coordinates image decoding, live-aware beatmap editing, and Slider Picturator mutation.</summary>
public sealed class SliderPicturatorService : ISliderPicturatorService
{
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IImageFileService _images;

    /// <summary>Creates the Slider Picturator application service.</summary>
    /// <param name="editingGateway">Loads and backup-saves beatmaps.</param>
    /// <param name="images">Decodes local image files into Core pixel buffers.</param>
    public SliderPicturatorService(IBeatmapEditingGateway editingGateway, IImageFileService images)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    /// <inheritdoc/>
    public async Task<SliderPicturatorResult> PicturateAsync(string path, SliderPicturatorOptions options,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        RgbaImage image = await _images.LoadAsync(options.PictureFile, cancellationToken).ConfigureAwait(false);
        progress?.Report(10);
        BeatmapEditingSession session = await _editingGateway.OpenBeatmapAsync(path, LiveBeatmapPreference.PreferLive, cancellationToken).ConfigureAwait(false);
        Beatmap beatmap = session.Editor.Beatmap;
        double circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
        RgbaColour sliderColour = options.UseMapComboColors ? options.ComboColor : options.CurrentTrackColor;
        double duration = options.SelectedSlider?.TemporalLength ?? options.Duration;
        RgbaColour backgroundColour = RgbaColour.FromRgb(0, 0, 0);
        (List<Mapping_Tools.Core.Classes.MathUtil.Vector2> pathPoints, double frameDistance) = SliderPicturatorEngine.Picturate(
            image, sliderColour, options.BorderColor, backgroundColour, circleSize,
            new(options.SliderStartX, options.SliderStartY), new(options.ImageStartX, options.ImageStartY),
            options.SelectedSlider, options.YResolution, options.ViewportSize, !options.BlackOn, !options.BorderOn,
            !options.AlphaOn, options.RedOn, options.GreenOn, options.BlueOn, options.Quality);
        cancellationToken.ThrowIfCancellationRequested();
        SliderPicturatorEngine.ApplyToBeatmap(beatmap, pathPoints, frameDistance, duration, options.TimeCode,
            sliderColour, options.BorderColor, options.SetBeatmapColors, options.UseMapComboColors);
        options.SegmentCount = SliderPicturatorEngine.Recolor(image, sliderColour, options.BorderColor,
            backgroundColour, options.SelectedSlider, !options.BlackOn, !options.BorderOn,
            !options.AlphaOn, options.RedOn, options.GreenOn, options.BlueOn, options.Quality).SegmentCount;
        await _editingGateway.SaveAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);
        progress?.Report(100);
        return new SliderPicturatorResult(path, options.SegmentCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RgbaColour>> GetAvailableColorsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BeatmapEditingSession session = await _editingGateway.OpenBeatmapAsync(path, LiveBeatmapPreference.PreferLive, cancellationToken).ConfigureAwait(false);
        Beatmap beatmap = session.Editor.Beatmap;
        IReadOnlyList<ComboColour> comboColours = beatmap.ComboColours.Count == 0
            ? ComboColour.GetDefaultComboColours()
            : beatmap.ComboColours;
        List<RgbaColour> colours = comboColours.Select(colour => colour.Color).ToList();
        if (beatmap.SpecialColours.TryGetValue("SliderTrackOverride", out ComboColour? overrideColour))
            colours.Add(overrideColour.Color);
        return colours;
    }

    /// <inheritdoc/>
    public async Task<HitObject?> GetSelectedSliderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BeatmapEditingSession session = await _editingGateway.OpenBeatmapAsync(path, LiveBeatmapPreference.RequireLive, cancellationToken).ConfigureAwait(false);
        return session.SelectedHitObjects.FirstOrDefault(item => item.IsSlider)?.DeepCopy();
    }
}
