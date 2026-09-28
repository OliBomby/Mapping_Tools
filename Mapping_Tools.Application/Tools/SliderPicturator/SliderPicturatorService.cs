using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Coordinates image decoding, live-aware beatmap editing, and Slider Picturator mutation.</summary>
public sealed class SliderPicturatorService : ISliderPicturatorService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IImageFileService images;
    private readonly ApplicationSettings settings;
    private readonly ILogger<SliderPicturatorService> logger;

    /// <summary>Creates the Slider Picturator application service.</summary>
    /// <param name="editingGateway">Loads and backup-saves beatmaps.</param>
    /// <param name="images">Decodes local image files into Core pixel buffers.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records image generation and save milestones.</param>
    public SliderPicturatorService(
        IBeatmapEditingGateway editingGateway,
        IImageFileService images,
        ApplicationSettings settings,
        ILogger<SliderPicturatorService>? logger = null)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<SliderPicturatorService>.Instance;
    }

    /// <inheritdoc />
    public async Task<SliderPicturatorResult> PicturateAsync(
        string path,
        SliderPicturatorServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Loading image {ImagePath} for beatmap {Path}", options.PictureFile, path);
        var image = await images.LoadAsync(options.PictureFile, cancellationToken).ConfigureAwait(false);
        progress?.Report(0.1);

        var session = await editingGateway.OpenBeatmapAsync(
            path,
            LiveBeatmapPreference.PreferLive,
            cancellationToken).ConfigureAwait(false);
        var beatmap = session.Beatmap;
        double circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
        (var pathPoints, double frameDistance) = SliderPicturatorEngine.Picturate(
            image,
            circleSize,
            options);
        logger.LogInformation("Generated path for {Path}; applying to beatmap", path);
        cancellationToken.ThrowIfCancellationRequested();

        SliderPicturatorEngine.ApplyToBeatmap(beatmap, pathPoints, frameDistance, options);
        long segmentCount = SliderPicturatorEngine.Recolor(image, options).SegmentCount;
        logger.LogInformation("Applied image to {Path} with {SegmentCount} segments; saving", path, segmentCount);

        await editingGateway
            .SaveAsync(
                session,
                AutomaticEditorReloadPolicy.ShouldReloadEditor(
                    session,
                    quickRun,
                    settings),
                cancellationToken)
            .ConfigureAwait(false);
        progress?.Report(1);
        logger.LogInformation("Completed {Path}", path);
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
            LiveBeatmapPreference.DiskOnly,
            cancellationToken).ConfigureAwait(false);
        var beatmap = session.Beatmap;
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

    private static void Validate(SliderPicturatorServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SliderPicturatorEngine.Validate(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PictureFile);
    }
}
