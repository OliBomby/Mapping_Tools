using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>
///     Selects objects, invokes the framework-independent slider engine, and saves
///     each changed beatmap through the editor gateway.
/// </summary>
public sealed class SliderCompletionatorService : ISliderCompletionatorService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;
    private readonly ILogger<SliderCompletionatorService> logger;

    /// <summary>
    ///     Creates a Slider Completionator service.
    /// </summary>
    /// <param name="editingGateway">Loads live-or-disk beatmaps and persists safe edits.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records selection and per-beatmap completion milestones.</param>
    public SliderCompletionatorService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings,
        ILogger<SliderCompletionatorService>? logger = null)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<SliderCompletionatorService>.Instance;
    }

    /// <inheritdoc />
    public async Task<SliderCompletionatorResult> CompleteAsync(
        IReadOnlyList<string> paths,
        SliderCompletionatorServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Validate(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(ApplicationStrings.Tools_AtLeastOneBeatmapRequired, nameof(paths));
        logger.LogInformation("Started for {Count} beatmaps; selection {Mode}", paths.Count, options.ImportModeSetting);

        List<string> processedPaths = [];
        int slidersCompleted = 0;
        List<(string Path, BeatmapEditingSession Session)> sessions = [];
        double? editorTime = null;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            var livePreference =
                options.ImportModeSetting == HitObjectSelectionMode.Selected
                    ? LiveBeatmapPreference.RequireLive
                    : LiveBeatmapPreference.PreferLive;
            var session = await editingGateway
                .OpenBeatmapAsync(path, livePreference, cancellationToken)
                .ConfigureAwait(false);

            if (options is { UseCurrentEditorTime: true, UseEndTime: true }) editorTime ??= session.LiveEditorTime;
            sessions.Add((path, session));
        }
        logger.LogInformation("Opened {Count} beatmaps; editor time {EditorTime}", sessions.Count, editorTime);

        if (options is { UseCurrentEditorTime: true, UseEndTime: true } && editorTime is null)
            throw new LiveBeatmapUnavailableException(
                "The current editor time could not be read.");

        for (int index = 0; index < sessions.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string path, var session) = sessions[index];

            var markedObjects = BeatmapObjectSelection.Select(
                session,
                options.ImportModeSetting,
                options.TimeCode);
            logger.LogInformation("Processing beatmap {Index}/{Count}: {Path}; {SelectedCount} selected objects",
                index + 1, paths.Count, path, markedObjects.Count);

            int completed = SliderCompletionatorEngine.Apply(
                session.Beatmap,
                markedObjects,
                options,
                editorTime,
                progress?.MapTo(index, paths.Count),
                cancellationToken);
            logger.LogInformation("Completed {CompletedCount} sliders in {Path}; saving", completed, path);
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
            slidersCompleted += completed;
        }

        progress?.Report(1);
        logger.LogInformation("Completed {Count} beatmaps with {CompletedCount} sliders completed",
            processedPaths.Count, slidersCompleted);
        return new SliderCompletionatorResult(processedPaths, slidersCompleted);
    }

    private static void Validate(SliderCompletionatorServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.ImportModeSetting))
            throw new ArgumentException(
                ApplicationStrings.SliderCompletionator_UnknownSelectionMode,
                nameof(options));
        SliderCompletionatorEngine.Validate(options);
    }
}
