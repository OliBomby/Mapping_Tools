using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.ComboColourStudio;

/// <summary>
///     Coordinates Combo Colour Studio's framework-neutral engine with beatmap
///     files, Editor Reader overlays, backups, and save progress.
/// </summary>
public sealed class ComboColourStudioService : IComboColourStudioService
{
    private readonly IBeatmapEditingGateway editing;
    private readonly ApplicationSettings settings;
    private readonly ILogger<ComboColourStudioService> logger;

    /// <summary>Creates a service using the shared beatmap editing gateway.</summary>
    /// <param name="editing">Opens and safely saves beatmaps.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records per-beatmap colour application milestones.</param>
    public ComboColourStudioService(
        IBeatmapEditingGateway editing,
        ApplicationSettings settings,
        ILogger<ComboColourStudioService>? logger = null)
    {
        this.editing = editing ?? throw new ArgumentNullException(nameof(editing));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<ComboColourStudioService>.Instance;
    }

    /// <inheritdoc />
    public async Task<ComboColourEngineOptions> ImportComboColoursAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var session = await editing.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken)
            .ConfigureAwait(false);

        return ComboColourStudioEngine.ImportComboColours(session.Beatmap);
    }

    /// <inheritdoc />
    public async Task<ComboColourEngineOptions> ImportColourHaxAsync(
        string path,
        int maxBurstLength,
        CancellationToken cancellationToken = default)
    {
        var session = await editing.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken)
            .ConfigureAwait(false);

        return ComboColourStudioEngine.ImportColourHax(session.Beatmap, maxBurstLength);
    }

    /// <inheritdoc />
    public async Task<ComboColourStudioRunResult> ApplyAsync(
        IReadOnlyList<string> paths,
        ComboColourServiceOptions project,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(project);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(ApplicationStrings.Tools_AtLeastOneBeatmapRequired, nameof(paths));
        ComboColourStudioEngine.Validate(project);
        logger.LogInformation("Started for {Count} beatmaps", paths.Count);

        int processed = 0;
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Processing beatmap {Index}/{Count}: {Path}", processed + 1, paths.Count, path);
            var session = await editing.OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            ComboColourStudioEngine.Apply(session.Beatmap, project);
            logger.LogInformation("Applied colours to {Path}; saving", path);
            cancellationToken.ThrowIfCancellationRequested();
            bool reloadEditor = AutomaticEditorReloadPolicy.ShouldReloadEditor(
                session,
                quickRun,
                settings);
            await editing.SaveAsync(session, reloadEditor, cancellationToken)
                .ConfigureAwait(false);

            processed++;
            progress?.Report(processed, paths.Count);
        }

        logger.LogInformation("Completed {Count} beatmaps", processed);
        return new ComboColourStudioRunResult(processed);
    }
}
