using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.SliderMerger;

namespace Mapping_Tools.Application.SliderMerger;

/// <summary>Selects Slider Merger inputs and persists its Core transformation.</summary>
public sealed class SliderMergerService : ISliderMergerService
{
    private readonly IBeatmapEditingGateway _editingGateway;

    /// <summary>Creates a Slider Merger service.</summary>
    /// <param name="editingGateway">Loads live-or-disk beatmaps and performs backup-safe saves.</param>
    public SliderMergerService(IBeatmapEditingGateway editingGateway)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc/>
    public async Task<SliderMergerResult> MergeAsync(
        IReadOnlyList<string> paths,
        SliderMergerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Select at least one beatmap.", nameof(paths));
        }

        if (!Enum.IsDefined(options.ImportModeSetting) ||
            !Enum.IsDefined(options.ConnectionModeSetting) ||
            !double.IsFinite(options.Leniency) ||
            options.Leniency < 0)
        {
            throw new ArgumentException("Slider Merger contains invalid settings.", nameof(options));
        }

        List<string> processedPaths = [];
        int objectsMerged = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            LiveBeatmapPreference preference = options.ImportModeSetting == SliderMergerImportMode.Selected
                ? LiveBeatmapPreference.RequireLive
                : LiveBeatmapPreference.PreferLive;
            BeatmapEditingSession session = await _editingGateway
                .OpenBeatmapAsync(path, preference, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<HitObject> markedObjects = BeatmapObjectSelection.Select(
                session,
                options.ImportModeSetting,
                SliderMergerImportMode.Selected,
                SliderMergerImportMode.Bookmarked,
                SliderMergerImportMode.Time,
                SliderMergerImportMode.Everything,
                options.TimeCode);
            Progress<double>? mapProgress = progress is null
                ? null
                : new Progress<double>(value => progress.Report((index * 100 + value) / paths.Count));
            objectsMerged += SliderMergerEngine.Merge(
                session.Editor.Beatmap,
                markedObjects,
                options,
                mapProgress,
                cancellationToken);
            await _editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
        }

        progress?.Report(100);
        return new SliderMergerResult(processedPaths, objectsMerged);
    }
}
