using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.SliderPicturator;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Coordinates image decoding, live-aware beatmap editing, and Slider Picturator mutation.</summary>
public sealed class SliderPicturatorService : ISliderPicturatorService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IImageFileService images;

    /// <summary>Creates the Slider Picturator application service.</summary>
    /// <param name="editingGateway">Loads and backup-saves beatmaps.</param>
    /// <param name="images">Decodes local image files into Core pixel buffers.</param>
    public SliderPicturatorService(IBeatmapEditingGateway editingGateway, IImageFileService images)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
    }

    /// <inheritdoc />
    public async Task<SliderPicturatorResult> PicturateAsync(
        string path,
        SliderPicturatorServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var image = await images.LoadAsync(options.PictureFile, cancellationToken).ConfigureAwait(false);
        progress?.Report(0.1);

        var session = await editingGateway.OpenBeatmapAsync(
            path,
            LiveBeatmapPreference.PreferLive,
            cancellationToken).ConfigureAwait(false);
        var beatmap = session.Editor.Beatmap;
        double circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
        (var pathPoints, double frameDistance) = SliderPicturatorEngine.Picturate(
            image,
            circleSize,
            options);
        cancellationToken.ThrowIfCancellationRequested();

        SliderPicturatorEngine.ApplyToBeatmap(beatmap, pathPoints, frameDistance, options);
        long segmentCount = SliderPicturatorEngine.Recolor(image, options).SegmentCount;

        await editingGateway.SaveAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);
        progress?.Report(1);
        return new SliderPicturatorResult(path, segmentCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RgbaColour>> GetAvailableColorsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var session = await editingGateway.OpenBeatmapAsync(
            path,
            LiveBeatmapPreference.PreferLive,
            cancellationToken).ConfigureAwait(false);
        var beatmap = session.Editor.Beatmap;
        IReadOnlyList<ComboColour> comboColours = beatmap.ComboColours.Count == 0
            ? ComboColour.GetDefaultComboColours()
            : beatmap.ComboColours;
        var colours = comboColours.Select(colour => colour.Color).ToList();
        if (beatmap.SpecialColours.TryGetValue("SliderTrackOverride", out var overrideColour))
            colours.Add(overrideColour.Color);
        return colours;
    }

    /// <inheritdoc />
    public async Task<HitObject?> GetSelectedSliderAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var session = await editingGateway.OpenBeatmapAsync(
            path,
            LiveBeatmapPreference.RequireLive,
            cancellationToken).ConfigureAwait(false);
        return session.SelectedHitObjects.FirstOrDefault(item => item.IsSlider)?.DeepCopy();
    }
}
