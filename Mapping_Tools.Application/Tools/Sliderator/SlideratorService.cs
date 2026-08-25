using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.Sliderator;

namespace Mapping_Tools.Application.Tools.Sliderator;

/// <summary>
///     Imports Sliderator sources and delegates geometry, backup, saving, and
///     optional reload behavior to the shared application boundaries.
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

    /// <inheritdoc />
    public async Task<SlideratorImportResult> ImportAsync(
        string path,
        HitObjectSelectionMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(mode)) throw new ArgumentException("Sliderator contains an unknown import mode.", nameof(mode));

        var preference = mode == HitObjectSelectionMode.Selected
            ? LiveBeatmapPreference.RequireLive
            : LiveBeatmapPreference.DiskOnly;
        var session = await editingGateway
            .OpenBeatmapAsync(path, preference, cancellationToken)
            .ConfigureAwait(false);
        var selected = BeatmapObjectSelection.Select(session, mode, timeCode);
        return new SlideratorImportResult(
            selected.Where(hitObject => hitObject.IsSlider).ToArray(),
            session.Editor.Beatmap.Difficulty["SliderMultiplier"].DoubleValue,
            session.Source == BeatmapEditingSource.LiveEditor,
            true);
    }

    /// <inheritdoc />
    public async Task<SlideratorResult> RunAsync(
        string path,
        SlideratorServiceOptions project,
        HitObject sourceSlider,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        bool preferLiveEditor = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sourceSlider);

        var session = await editingGateway
            .OpenBeatmapAsync(
                path,
                preferLiveEditor ? LiveBeatmapPreference.PreferLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        // Do Sliderator
        var applied = SlideratorEngine.Apply(
            session.Editor.Beatmap,
            sourceSlider,
            project,
            progress,
            cancellationToken);
        bool shouldReload = reloadEditor && session.Source == BeatmapEditingSource.LiveEditor;
        // Save the file
        await editingGateway
            .SaveAsync(session, shouldReload, cancellationToken)
            .ConfigureAwait(false);
        progress?.Report(1);
        return new SlideratorResult(path, applied, shouldReload);
    }
}
