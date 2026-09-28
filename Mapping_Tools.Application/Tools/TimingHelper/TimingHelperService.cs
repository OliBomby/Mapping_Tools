using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.TimingHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.Tools.TimingHelper;

/// <summary>
///     Coordinates live-aware beatmap loading, Timing Helper transformation, and
///     backup-safe persistence.
/// </summary>
public sealed class TimingHelperService : ITimingHelperService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;
    private readonly ILogger<TimingHelperService> logger;

    /// <summary>
    ///     Creates the Timing Helper application service.
    /// </summary>
    /// <param name="editingGateway">Loads and saves beatmaps through the shared backup boundary.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records per-beatmap timing changes.</param>
    public TimingHelperService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings,
        ILogger<TimingHelperService>? logger = null)
    {
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<TimingHelperService>.Instance;
    }

    /// <inheritdoc />
    public async Task<TimingHelperResult> AdjustAsync(
        IReadOnlyList<string> paths,
        TimingHelperServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));
        TimingHelperEngine.Validate(options);
        logger.LogInformation("Started for {Count} beatmaps", paths.Count);

        List<string> processedPaths = [];
        int redlinesAdded = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            logger.LogInformation("Processing beatmap {Index}/{Count}: {Path}", index + 1, paths.Count, path);
            int pathIndex = index;
            var session = await editingGateway
                .OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);

            var mapProgress = progress?.MapTo(pathIndex, paths.Count);
            int added = TimingHelperEngine.Apply(
                session.Beatmap,
                options,
                mapProgress,
                cancellationToken);
            redlinesAdded += added;
            logger.LogInformation("Added {RedlinesAdded} redlines to {Path}; saving", added, path);
            await editingGateway
                .SaveAsync(
                    session,
                    AutomaticEditorReloadPolicy.ShouldReloadEditor(
                        session,
                        quickRun,
                        settings),
                    cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
            progress?.Report(index + 1, paths.Count);
        }

        logger.LogInformation("Completed {Count} beatmaps with {RedlinesAdded} redlines added", processedPaths.Count, redlinesAdded);
        return new TimingHelperResult(processedPaths, redlinesAdded);
    }
}
