using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

namespace Mapping_Tools.Application.Tools.HitsoundPreviewHelper;

/// <summary>
///     Invokes the Core hitsound engine for every object and persists each
///     changed map through the backup-aware editor gateway.
/// </summary>
public sealed class HitsoundPreviewHelperService : IHitsoundPreviewHelperService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>Creates the hitsound-preview application service.</summary>
    /// <param name="editingGateway">Loads live-or-disk maps and saves safe edits.</param>
    public HitsoundPreviewHelperService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<HitsoundPreviewHelperResult> ApplyAsync(
        IReadOnlyList<string> paths,
        HitsoundPreviewHelperServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Validate(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        List<string> processedPaths = [];
        int updatedEventCount = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var session = await editingGateway
                .OpenBeatmapAsync(
                    paths[index],
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);

            int updated = HitsoundPreviewHelperEngine.Apply(
                session.Editor.Beatmap,
                options.Items,
                progress?.MapTo(index, paths.Count),
                cancellationToken);

            // Save the file
            await editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(paths[index]);
            updatedEventCount += updated;
        }

        progress?.Report(1);
        return new HitsoundPreviewHelperResult(processedPaths, updatedEventCount);
    }

    private static void Validate(HitsoundPreviewHelperServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        HitsoundPreviewHelperEngine.Validate(options.Items);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var session = await editingGateway
            .OpenBeatmapAsync(path, LiveBeatmapPreference.RequireLive, cancellationToken)
            .ConfigureAwait(false);
        bool mania = session.Editor.Beatmap.General["Mode"].IntValue == 3;
        return session.SelectedHitObjects
            .Select(hitObject => new Vector2(hitObject.Pos.X, mania ? -1 : hitObject.Pos.Y))
            .Distinct()
            .ToArray();
    }
}
