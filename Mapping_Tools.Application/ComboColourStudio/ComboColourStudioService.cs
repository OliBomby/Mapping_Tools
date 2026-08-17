using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.ComboColourStudio;

namespace Mapping_Tools.Application.ComboColourStudio;

/// <summary>
/// Coordinates Combo Colour Studio's framework-neutral engine with beatmap
/// files, Editor Reader overlays, backups, and save progress.
/// </summary>
public sealed class ComboColourStudioService : IComboColourStudioService
{
    private readonly IBeatmapEditingGateway _editing;

    /// <summary>Creates a service using the shared beatmap editing gateway.</summary>
    /// <param name="editing">Opens and safely saves beatmaps.</param>
    public ComboColourStudioService(IBeatmapEditingGateway editing)
    {
        _editing = editing ?? throw new ArgumentNullException(nameof(editing));
    }

    /// <inheritdoc/>
    public async Task ImportComboColoursAsync(
        string path,
        ComboColourProject project,
        CancellationToken cancellationToken = default)
    {
        BeatmapEditingSession session = await _editing.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        ComboColourStudioEngine.ImportComboColours(session.Editor.Beatmap, project);
    }

    /// <inheritdoc/>
    public async Task ImportColourHaxAsync(
        string path,
        ComboColourProject project,
        CancellationToken cancellationToken = default)
    {
        BeatmapEditingSession session = await _editing.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        ComboColourStudioEngine.ImportColourHax(session.Editor.Beatmap, project);
    }

    /// <inheritdoc/>
    public async Task<ComboColourStudioRunResult> ApplyAsync(
        IReadOnlyList<string> paths,
        ComboColourProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(project);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Select at least one beatmap.", nameof(paths));
        }

        IReadOnlyList<string> errors = project.ValidateForExport();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(project));
        }

        int processed = 0;
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditingSession session = await _editing.OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            ComboColourStudioEngine.Apply(session.Editor.Beatmap, project);
            cancellationToken.ThrowIfCancellationRequested();
            await _editing.SaveAsync(session, reloadEditor: false, cancellationToken)
                .ConfigureAwait(false);

            processed++;
            progress?.Report(processed * 100d / paths.Count);
        }

        return new ComboColourStudioRunResult(processed);
    }
}
