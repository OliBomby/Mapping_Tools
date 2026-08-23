using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.TimingHelper;

namespace Mapping_Tools.Application.TimingHelper;

/// <summary>
///     Coordinates live-aware beatmap loading, Timing Helper transformation, and
///     backup-safe persistence.
/// </summary>
public sealed class TimingHelperService : ITimingHelperService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>
    ///     Creates the Timing Helper application service.
    /// </summary>
    /// <param name="editingGateway">Loads and saves beatmaps through the shared backup boundary.</param>
    public TimingHelperService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<TimingHelperResult> AdjustAsync(
        IReadOnlyList<string> paths,
        TimingHelperOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        List<string> processedPaths = [];
        int redlinesAdded = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            int pathIndex = index;
            var session = await editingGateway
                .OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);

            Progress<double> mapProgress = new(value =>
                progress?.Report((pathIndex * 100d + value) / paths.Count));
            redlinesAdded += TimingHelperEngine.Apply(
                session.Editor.Beatmap,
                options,
                mapProgress,
                cancellationToken);
            await editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(path);
            progress?.Report((index + 1) * 100d / paths.Count);
        }

        return new TimingHelperResult(processedPaths, redlinesAdded);
    }
}
