using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.Sliderator;

namespace Mapping_Tools.Application.Sliderator;

/// <summary>
/// Imports Sliderator sources and delegates geometry, backup, saving, and
/// optional reload behavior to the shared application boundaries.
/// </summary>
public sealed class SlideratorService : ISlideratorService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>Creates the Sliderator application service.</summary>
    /// <param name="editingGateway">Opens live-or-disk maps and saves backup-first.</param>
    public SlideratorService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc/>
    public async Task<SlideratorImportResult> ImportAsync(
        string path,
        SlideratorImportMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentException("Sliderator contains an unknown import mode.", nameof(mode));
        }

        LiveBeatmapPreference preference = mode == SlideratorImportMode.Selected
            ? LiveBeatmapPreference.RequireLive
            : LiveBeatmapPreference.DiskOnly;
        BeatmapEditingSession session = await editingGateway
            .OpenBeatmapAsync(path, preference, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<Mapping_Tools.Core.Classes.BeatmapHelper.HitObject> selected = mode switch
        {
            SlideratorImportMode.Selected => session.SelectedHitObjects,
            SlideratorImportMode.Bookmarked => session.Editor.Beatmap.GetBookmarkedObjects(),
            SlideratorImportMode.Time => session.Editor.Beatmap.QueryTimeCode(timeCode ?? string.Empty).ToList(),
            _ => throw new ArgumentException("Sliderator contains an unknown import mode.", nameof(mode))
        };
        return new SlideratorImportResult(
            selected.Where(hitObject => hitObject.IsSlider).ToArray(),
            session.Editor.Beatmap.Difficulty["SliderMultiplier"].DoubleValue,
            session.Source == BeatmapEditingSource.LiveEditor,
            true);
    }

    /// <inheritdoc/>
    public async Task<SlideratorResult> RunAsync(
        string path,
        SlideratorProject project,
        Mapping_Tools.Core.Classes.BeatmapHelper.HitObject sourceSlider,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sourceSlider);

        BeatmapEditingSession session = await editingGateway
            .OpenBeatmapAsync(
                path,
                project.DoEditorRead ? LiveBeatmapPreference.PreferLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        project.DoEditorRead = false;
        SlideratorApplyResult applied = SlideratorEngine.Apply(
            session.Editor.Beatmap,
            sourceSlider,
            project,
            progress,
            cancellationToken);
        bool shouldReload = reloadEditor && session.Source == BeatmapEditingSource.LiveEditor;
        await editingGateway
            .SaveAsync(session, shouldReload, cancellationToken)
            .ConfigureAwait(false);
        progress?.Report(100);
        return new SlideratorResult(path, applied, shouldReload);
    }
}
