using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

namespace Mapping_Tools.Application.HitsoundPreviewHelper;

/// <summary>
///     Selects eligible objects, invokes the Core hitsound engine, and persists
///     each changed map through the backup-aware editor gateway.
/// </summary>
public sealed class HitsoundPreviewHelperService : IHitsoundPreviewHelperService
{
    private readonly IBeatmapEditingGateway _editingGateway;

    /// <summary>Creates the hitsound-preview application service.</summary>
    /// <param name="editingGateway">Loads live-or-disk maps and saves safe edits.</param>
    public HitsoundPreviewHelperService(IBeatmapEditingGateway editingGateway)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<HitsoundPreviewHelperResult> ApplyAsync(
        IReadOnlyList<string> paths,
        HitsoundPreviewHelperOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        if (!Enum.IsDefined(options.ImportModeSetting))
            throw new ArgumentException(
                "Hitsound Preview Helper contains an unknown object-selection mode.",
                nameof(options));

        if (options.Items is null || options.Items.Count == 0) throw new ArgumentException("There are no zones!", nameof(options));

        if (options.ImportModeSetting == HitsoundPreviewHelperImportMode.Time && string.IsNullOrWhiteSpace(options.TimeCode))
            throw new ArgumentException(
                "A time code is required for Time mode.",
                nameof(options));

        if (options.Items.Any(zone =>
                !Enum.IsDefined(zone.Hitsound) || !Enum.IsDefined(zone.SampleSet) || !Enum.IsDefined(zone.AdditionsSet)))
            throw new ArgumentException(
                "Hitsound Preview Helper contains an unknown hitsound or sample-set value.",
                nameof(options));

        List<string> processedPaths = [];
        int updatedEventCount = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var livePreference =
                options.ImportModeSetting == HitsoundPreviewHelperImportMode.Selected
                    ? LiveBeatmapPreference.RequireLive
                    : LiveBeatmapPreference.PreferLive;
            var session = await _editingGateway
                .OpenBeatmapAsync(paths[index], livePreference, cancellationToken)
                .ConfigureAwait(false);

            var markedObjects =
                BeatmapObjectSelection.Select(
                    session,
                    options.ImportModeSetting,
                    HitsoundPreviewHelperImportMode.Selected,
                    HitsoundPreviewHelperImportMode.Bookmarked,
                    HitsoundPreviewHelperImportMode.Time,
                    HitsoundPreviewHelperImportMode.Everything,
                    options.TimeCode);
            int updated = HitsoundPreviewHelperEngine.Apply(
                session.Editor.Beatmap,
                markedObjects,
                options.Items,
                new Progress<double>(value => progress?.Report(
                    (index + value / 100) / paths.Count * 100)),
                cancellationToken);

            // Save the file
            await _editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(paths[index]);
            updatedEventCount += updated;
        }

        progress?.Report(100);
        return new HitsoundPreviewHelperResult(processedPaths, updatedEventCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var session = await _editingGateway
            .OpenBeatmapAsync(path, LiveBeatmapPreference.RequireLive, cancellationToken)
            .ConfigureAwait(false);
        bool mania = session.Editor.Beatmap.General["Mode"].IntValue == 3;
        return session.SelectedHitObjects
            .Select(hitObject => new Vector2(hitObject.Pos.X, mania ? -1 : hitObject.Pos.Y))
            .Distinct()
            .ToArray();
    }
}
