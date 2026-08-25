using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.Tools.MetadataManager;

/// <summary>
///     Coordinates live-aware target loading, safety copies, metadata transformation,
///     and metadata-derived output filenames.
/// </summary>
public sealed class MetadataManagerService : IMetadataManagerService
{
    private readonly IBeatmapBackupService backupService;
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>
    ///     Creates a Metadata Manager application service.
    /// </summary>
    /// <param name="editingGateway">Loads beatmaps with the configured live-editor preference.</param>
    /// <param name="backupService">Creates the pre-write backup for each target.</param>
    public MetadataManagerService(
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    /// <inheritdoc />
    public async Task<MetadataManagerOptions> ImportAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var session = await editingGateway
            .OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
            .ConfigureAwait(false);
        return MetadataManagerEngine.Read(session.Editor.Beatmap);
    }

    /// <inheritdoc />
    public async Task<MetadataManagerResult> ExportAsync(
        MetadataManagerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        string[] paths = options.ExportPath
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0)
            throw new ArgumentException(
                "Select at least one target beatmap.",
                nameof(options));

        List<string> processedPaths = [];
        for (int index = 0; index < paths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var session = await editingGateway
                .OpenBeatmapAsync(
                    paths[index],
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            await backupService.CreateAsync(
                    session,
                    BeatmapBackupReason.Automatic,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);

            MetadataManagerEngine.Apply(session.Editor.Beatmap, options);
            cancellationToken.ThrowIfCancellationRequested();
            // Save the file with name update because we updated the metadata
            session.Editor.SaveFileWithNameUpdate();
            processedPaths.Add(session.Editor.Path);
            progress?.Report(index + 1, paths.Length);
        }

        return new MetadataManagerResult(processedPaths);
    }
}
