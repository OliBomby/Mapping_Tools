using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.SliderMerger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Selects Slider Merger inputs and persists its Core transformation.</summary>
public sealed class SliderMergerService : ISliderMergerService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;
    private readonly ILogger<SliderMergerService> logger;

    /// <summary>Creates a Slider Merger service.</summary>
    /// <param name="editingGateway">Loads live-or-disk beatmaps and performs backup-safe saves.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records per-beatmap merge milestones.</param>
    public SliderMergerService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings,
        ILogger<SliderMergerService>? logger = null)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<SliderMergerService>.Instance;
    }

    /// <inheritdoc />
    public async Task<SliderMergerResult> MergeAsync(
        IReadOnlyList<string> paths,
        SliderMergerServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Validate(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(ApplicationStrings.Tools_AtLeastOneBeatmapRequired, nameof(paths));
        logger.LogInformation("Started for {Count} beatmaps; selection {Mode}", paths.Count, options.ImportModeSetting);

        List<string> processedPaths = [];
        int objectsMerged = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            logger.LogInformation("Processing beatmap {Index}/{Count}: {Path}", index + 1, paths.Count, path);
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            var preference = options.ImportModeSetting == HitObjectSelectionMode.Selected
                ? LiveBeatmapPreference.RequireLive
                : LiveBeatmapPreference.PreferLive;
            var session = await editingGateway
                .OpenBeatmapAsync(path, preference, cancellationToken)
                .ConfigureAwait(false);
            var markedObjects = BeatmapObjectSelection.Select(
                session,
                options.ImportModeSetting,
                options.TimeCode);
            var mapProgress = progress?.MapTo(index, paths.Count);
            int merged = SliderMergerEngine.Merge(
                session.Beatmap,
                markedObjects,
                options,
                mapProgress,
                cancellationToken);
            objectsMerged += merged;
            logger.LogInformation("Merged {MergedCount} objects in {Path} from {SelectedCount} selected objects",
                merged, path, markedObjects.Count);
            // Save the file
            await editingGateway
                .SaveAsync(
                    session,
                    AutomaticEditorReloadPolicy.ShouldReloadEditor(
                        session,
                        quickRun,
                        settings),
                    cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
        }

        progress?.Report(1);
        logger.LogInformation("Completed {Count} beatmaps with {MergedCount} objects merged", processedPaths.Count, objectsMerged);
        return new SliderMergerResult(processedPaths, objectsMerged);
    }

    private static void Validate(SliderMergerServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.ImportModeSetting))
            throw new ArgumentException(ApplicationStrings.SliderMerger_UnknownImportMode, nameof(options));
        SliderMergerEngine.Validate(options);
    }
}
