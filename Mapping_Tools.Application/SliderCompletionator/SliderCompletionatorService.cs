using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.SliderCompletionator;

namespace Mapping_Tools.Application.SliderCompletionator;

/// <summary>
///     Selects objects, invokes the framework-independent slider engine, and saves
///     each changed beatmap through the editor gateway.
/// </summary>
public sealed class SliderCompletionatorService : ISliderCompletionatorService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>
    ///     Creates a Slider Completionator service.
    /// </summary>
    /// <param name="editingGateway">Loads live-or-disk beatmaps and persists safe edits.</param>
    public SliderCompletionatorService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<SliderCompletionatorResult> CompleteAsync(
        IReadOnlyList<string> paths,
        SliderCompletionatorOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        if (!Enum.IsDefined(options.ImportModeSetting) || !Enum.IsDefined(options.FreeVariableSetting))
            throw new ArgumentException(
                "Slider Completionator contains an unknown selection or free-variable mode.",
                nameof(options));

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
                options.ImportModeSetting == SliderCompletionatorImportMode.Selected
                    ? LiveBeatmapPreference.RequireLive
                    : LiveBeatmapPreference.PreferLive;
            var session = await editingGateway
                .OpenBeatmapAsync(path, livePreference, cancellationToken)
                .ConfigureAwait(false);

            if (options.UseCurrentEditorTime && options.UseEndTime) editorTime ??= session.LiveEditorTime;
            sessions.Add((path, session));
        }

        if (options.UseCurrentEditorTime && options.UseEndTime && editorTime is null)
            throw new LiveBeatmapUnavailableException(
                "The current editor time could not be read.");

        for (int index = 0; index < sessions.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string path, var session) = sessions[index];

            var markedObjects =
                BeatmapObjectSelection.Select(
                    session,
                    options.ImportModeSetting,
                    SliderCompletionatorImportMode.Selected,
                    SliderCompletionatorImportMode.Bookmarked,
                    SliderCompletionatorImportMode.Time,
                    SliderCompletionatorImportMode.Everything,
                    options.TimeCode);

            int completed = SliderCompletionatorEngine.Apply(
                session.Editor.Beatmap,
                markedObjects,
                options,
                editorTime,
                new Progress<double>(value => progress?.Report(
                    (index + value / 100) / paths.Count * 100)),
                cancellationToken);
            // Save the file
            await editingGateway
                .SaveAsync(
                    session,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
            slidersCompleted += completed;
        }

        progress?.Report(100);
        return new SliderCompletionatorResult(processedPaths, slidersCompleted);
    }
}
