using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.MetadataManager;

/// <summary>
/// Coordinates live-aware target loading, safety copies, metadata transformation,
/// and metadata-derived output filenames.
/// </summary>
public sealed class MetadataManagerService : IMetadataManagerService
{
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IBeatmapBackupService _backupService;
    private readonly ITextFileStore _fileStore;

    /// <summary>
    /// Creates a Metadata Manager application service.
    /// </summary>
    /// <param name="editingGateway">Loads beatmaps with the configured live-editor preference.</param>
    /// <param name="backupService">Creates the pre-write safety copy for each target.</param>
    /// <param name="fileStore">Persists renamed output files and resolves their parent paths.</param>
    public MetadataManagerService(
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService,
        ITextFileStore fileStore)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    /// <inheritdoc/>
    public async Task<MetadataManagerOptions> ImportAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BeatmapEditingSession session = await _editingGateway
            .OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
            .ConfigureAwait(false);
        return MetadataManagerEngine.Read(session.Editor.Beatmap);
    }

    /// <inheritdoc/>
    public async Task<MetadataManagerResult> ExportAsync(
        MetadataManagerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        string[] paths = options.ExportPath
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0)
        {
            throw new ArgumentException(
                "Select at least one target beatmap.",
                nameof(options));
        }

        List<string> processedPaths = [];
        for (int index = 0; index < paths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditingSession session = await _editingGateway
                .OpenBeatmapAsync(
                    paths[index],
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            await _backupService.CreateAsync(
                    session,
                    BeatmapBackupReason.Automatic,
                    force: false,
                    cancellationToken)
                .ConfigureAwait(false);

            MetadataManagerEngine.Apply(session.Editor.Beatmap, options);
            cancellationToken.ThrowIfCancellationRequested();
            string outputPath = SaveWithNameUpdate(session.Editor);
            processedPaths.Add(outputPath);
            progress?.Report((index + 1) * 100d / paths.Length);
        }

        return new MetadataManagerResult(processedPaths);
    }

    private string SaveWithNameUpdate(BeatmapEditor2 editor)
    {
        string originalPath = editor.Path;
        string outputName = editor.Beatmap.GetFileName();
        string outputPath = _fileStore.CombinePath(
            _fileStore.GetParentFolder(originalPath),
            outputName);

        if (string.Equals(originalPath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            editor.SaveFile();
            return originalPath;
        }

        editor.SaveFile(outputPath);
        _fileStore.Delete(originalPath);
        editor.Path = outputPath;
        return outputPath;
    }
}
