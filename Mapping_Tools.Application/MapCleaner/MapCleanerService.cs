using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Application.MapCleaner;

/// <summary>Runs cleaner transformations over live-aware sessions and persists each through safety copies.</summary>
public sealed class MapCleanerService : IMapCleanerService
{
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IBeatmapFileSystem _fileSystem;
    private readonly IMapCleanerSampleService _samples;

    /// <summary>Creates a service that cleans beatmaps and their mapset samples.</summary>
    /// <param name="editingGateway">The live-aware, backup-before-write beatmap gateway.</param>
    /// <param name="fileSystem">Resolves beatmap parent directories.</param>
    /// <param name="samples">Analyzes and recoverably removes mapset samples.</param>
    public MapCleanerService(
        IBeatmapEditingGateway editingGateway,
        IBeatmapFileSystem fileSystem,
        IMapCleanerSampleService samples)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _samples = samples ?? throw new ArgumentNullException(nameof(samples));
    }

    /// <inheritdoc/>
    public async Task<MapCleanerResult> CleanAsync(
        IReadOnlyList<string> paths,
        MapCleanerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Select at least one beatmap.", nameof(paths));
        }

        MapCleanerResult total = new(0, 0, 0, [], [], [], 20);
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            string directory = _fileSystem.GetParentDirectory(path) ??
                throw new InvalidOperationException($"Could not resolve the folder for '{path}'.");
            BeatmapEditingSession session = await _editingGateway.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            IReadOnlyDictionary<string, string> samples = await _samples.AnalyzeAsync(
                directory,
                options.AnalyzeSamples,
                cancellationToken).ConfigureAwait(false);
            Progress<double>? mapProgress = progress is null ? null : new Progress<double>(value =>
                progress.Report((index * 100 + value) / paths.Count));
            MapCleanerResult result = Mapping_Tools.Core.Tools.MapCleaner.MapCleanerEngine.Clean(
                session.Editor.Beatmap,
                options,
                directory,
                samples,
                mapProgress,
                cancellationToken);
            // Save the file
            await _editingGateway.SaveAsync(
                session,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            int removedSamples = options.RemoveUnusedSamples
                ? await _samples.MoveUnusedToRecoveryAsync(
                    directory,
                    path,
                    session.Editor.Beatmap,
                    cancellationToken).ConfigureAwait(false)
                : 0;
            // Update result with removed count
            total = total.Add(result with { SamplesRemoved = removedSamples });
        }
        return total;
    }
}
