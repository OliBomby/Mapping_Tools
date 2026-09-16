using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
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
    private readonly ApplicationSettings settings;

    /// <summary>Creates the Sliderator application service.</summary>
    /// <param name="editingGateway">Opens live-or-disk maps and saves backup-first.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    public SlideratorService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
            session.Beatmap.Difficulty["SliderMultiplier"].DoubleValue,
            session.Source == BeatmapEditingSource.LiveEditor,
            true);
    }

    /// <inheritdoc />
    public async Task<SlideratorResult> RunAsync(
        string path,
        SlideratorServiceOptions project,
        HitObject sourceSlider,
        bool quickRun,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        bool preferLiveEditor = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sourceSlider);
        SlideratorEngine.Validate(project, sourceSlider);

        var session = await editingGateway
            .OpenBeatmapAsync(
                path,
                preferLiveEditor ? LiveBeatmapPreference.PreferLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        // Do Sliderator
        var applied = SlideratorEngine.Apply(
            session.Beatmap,
            sourceSlider,
            project,
            progress,
            cancellationToken);
        bool shouldReload = AutomaticEditorReloadPolicy.ShouldReloadEditor(
            session,
            quickRun,
            settings);
        // Save the file
        await editingGateway
            .SaveAsync(session, shouldReload, cancellationToken)
            .ConfigureAwait(false);
        progress?.Report(1);
        return new SlideratorResult(path, applied, shouldReload);
    }
}
