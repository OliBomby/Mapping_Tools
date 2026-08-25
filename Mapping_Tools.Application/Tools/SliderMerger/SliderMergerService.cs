using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.SliderMerger;
using Mapping_Tools.Core.Tools.SliderMerger.Models;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Selects Slider Merger inputs and persists its Core transformation.</summary>
public sealed class SliderMergerService : ISliderMergerService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>Creates a Slider Merger service.</summary>
    /// <param name="editingGateway">Loads live-or-disk beatmaps and performs backup-safe saves.</param>
    public SliderMergerService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<SliderMergerResult> MergeAsync(
        IReadOnlyList<string> paths,
        SliderMergerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        if (!Enum.IsDefined(options.ImportModeSetting) || !Enum.IsDefined(options.ConnectionModeSetting) || !double.IsFinite(options.Leniency) || options.Leniency < 0)
            throw new ArgumentException("Slider Merger contains invalid settings.", nameof(options));

        List<string> processedPaths = [];
        int objectsMerged = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
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
            objectsMerged += SliderMergerEngine.Merge(
                session.Editor.Beatmap,
                markedObjects,
                options,
                mapProgress,
                cancellationToken);
            // Save the file
            await editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
        }

        progress?.Report(1);
        return new SliderMergerResult(processedPaths, objectsMerged);
    }
}
