using System.Security.Cryptography;
using System.Text;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Backups;

/// <summary>
/// Enforces backup-before-overwrite ordering and preserves legacy-compatible
/// names and retention while leaving physical I/O to Infrastructure.
/// </summary>
public sealed class BeatmapBackupService : IBeatmapBackupService
{
    private readonly IBeatmapBackupStore _store;
    private readonly ITextFileStore _textFileStore;
    private readonly IEditorReloadService _reloadService;
    private readonly ApplicationSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, string> _periodicHashes =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    /// <summary>
    /// Creates a process-lifetime backup coordinator whose serialization lock
    /// prevents same-second requests from racing over legacy-compatible names.
    /// </summary>
    /// <param name="store">Physical copy, write, enumeration, and pruning operations.</param>
    /// <param name="textFileStore">Persistence used to validate beatmap metadata without direct filesystem access.</param>
    /// <param name="reloadService">The osu! refresh port used only after a successful restore.</param>
    /// <param name="settings">The current backup directory, enablement, and retention policy.</param>
    /// <param name="timeProvider">Supplies deterministic local timestamps for filenames and tests.</param>
    public BeatmapBackupService(
        IBeatmapBackupStore store,
        ITextFileStore textFileStore,
        IEditorReloadService reloadService,
        ApplicationSettings settings,
        TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
        _reloadService = reloadService ?? throw new ArgumentNullException(nameof(reloadService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public Task<BeatmapBackupResult> CreateAsync(
        IEnumerable<string> sourcePaths,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        return CreateFilesAsync(
            sourcePaths,
            reason,
            force,
            [],
            cancellationToken);
    }

    private async Task<BeatmapBackupResult> CreateFilesAsync(
        IEnumerable<string> sourcePaths,
        BeatmapBackupReason reason,
        bool force,
        IReadOnlyCollection<string> additionallyProtectedPaths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        string[] paths = sourcePaths.ToArray();
        if (paths.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Backup source paths cannot contain an empty value.",
                nameof(sourcePaths));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!force && !_settings.MakeBackups)
        {
            return new BeatmapBackupResult([], SkippedByPreference: true);
        }

        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureBackupDirectory();
            DateTimeOffset createdAt = _timeProvider.GetLocalNow();
            List<BeatmapBackupArtifact> artifacts = [];
            foreach (string path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                artifacts.Add(
                    await CopySourceAsync(
                            path,
                            reason,
                            createdAt,
                            cancellationToken)
                        .ConfigureAwait(false));
            }

            // Delete old files if the number of backup files are over the limit
            await PruneAsync(
                    artifacts
                        .Select(artifact => artifact.Path)
                        .Concat(additionallyProtectedPaths),
                    cancellationToken)
                .ConfigureAwait(false);
            return new BeatmapBackupResult(artifacts, SkippedByPreference: false);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<BeatmapBackupResult> CreateAsync(
        BeatmapEditingSession session,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!force && !_settings.MakeBackups)
        {
            return new BeatmapBackupResult([], SkippedByPreference: true);
        }

        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureBackupDirectory();
            DateTimeOffset createdAt = _timeProvider.GetLocalNow();
            BeatmapBackupArtifact disk = await CopySourceAsync(
                    session.Editor.Path,
                    reason,
                    createdAt,
                    cancellationToken)
                .ConfigureAwait(false);
            List<BeatmapBackupArtifact> artifacts = [disk];

            if (session.Source == BeatmapEditingSource.LiveEditor &&
                !HasSameContentsAsDisk(session.Editor.Path, session.InitialBeatmapLines))
            {
                // Save second copy with newest version if possible
                artifacts.Add(
                    await WriteSnapshotAsync(
                            session.Editor.Path,
                            session.InitialBeatmapLines,
                            reason,
                            createdAt,
                            liveCompanion: true,
                            cancellationToken)
                        .ConfigureAwait(false));
            }

            await PruneAsync(
                    artifacts.Select(artifact => artifact.Path),
                    cancellationToken)
                .ConfigureAwait(false);
            return new BeatmapBackupResult(artifacts, SkippedByPreference: false);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private bool HasSameContentsAsDisk(
        string path,
        IReadOnlyList<string> serializedLines)
    {
        IReadOnlyList<string> diskLines = _textFileStore.ReadAllLines(path);
        return diskLines.SequenceEqual(serializedLines, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_settings.MakePeriodicBackups)
        {
            return null;
        }

        IReadOnlyList<string> lines = session.Editor.Beatmap.GetLines();
        string hash = ComputeHash(lines);
        cancellationToken.ThrowIfCancellationRequested();

        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_periodicHashes.TryGetValue(session.Editor.Path, out string? previous) &&
                string.Equals(previous, hash, StringComparison.Ordinal))
            {
                return null;
            }

            EnsureBackupDirectory();
            DateTimeOffset createdAt = _timeProvider.GetLocalNow();
            // Save temp version
            BeatmapBackupArtifact artifact = await WriteSnapshotAsync(
                    session.Editor.Path,
                    lines,
                    BeatmapBackupReason.Periodic,
                    createdAt,
                    liveCompanion: false,
                    cancellationToken)
                .ConfigureAwait(false);
            _periodicHashes[session.Editor.Path] = hash;
            await PruneAsync([artifact.Path], cancellationToken)
                .ConfigureAwait(false);
            return artifact;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<BeatmapRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRestore(backupPath, destinationPath, allowDifferentFilename);
        cancellationToken.ThrowIfCancellationRequested();
        BeatmapBackupResult safety = await CreateFilesAsync(
                [destinationPath],
                BeatmapBackupReason.RestoreSafety,
                force: true,
                [backupPath],
                cancellationToken)
            .ConfigureAwait(false);
        BeatmapBackupArtifact safetyArtifact = safety.Artifacts.Single();

        cancellationToken.ThrowIfCancellationRequested();
        await _store.CopyAsync(
                backupPath,
                destinationPath,
                cancellationToken)
            .ConfigureAwait(false);

        if (reloadEditor)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _reloadService.ReloadAsync(cancellationToken).ConfigureAwait(false);
        }

        return new BeatmapRestoreResult(
            backupPath,
            destinationPath,
            safetyArtifact);
    }

    /// <inheritdoc/>
    public async Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        EnsureBackupDirectory();
        IReadOnlyList<StoredBeatmapBackup> backups = await _store
            .ListAsync(_settings.BackupsPath, cancellationToken)
            .ConfigureAwait(false);
        StoredBeatmapBackup? newest = backups.FirstOrDefault();
        if (newest is null)
        {
            return null;
        }

        return await RestoreAsync(
                newest.Path,
                destinationPath,
                allowDifferentFilename,
                reloadEditor,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<BeatmapBackupArtifact> CopySourceAsync(
        string sourcePath,
        BeatmapBackupReason reason,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (!_store.FileExists(sourcePath))
        {
            throw new FileNotFoundException(
                "The beatmap selected for backup does not exist.",
                sourcePath);
        }

        string destination = CreateDestination(
            sourcePath,
            reason,
            createdAt,
            liveCompanion: false);
        // Save normal copy
        await _store.CopyAsync(
                sourcePath,
                destination,
                cancellationToken)
            .ConfigureAwait(false);
        return new BeatmapBackupArtifact(
            destination,
            sourcePath,
            reason,
            ContainsUnsavedState: false,
            createdAt);
    }

    private async Task<BeatmapBackupArtifact> WriteSnapshotAsync(
        string sourcePath,
        IReadOnlyList<string> lines,
        BeatmapBackupReason reason,
        DateTimeOffset createdAt,
        bool liveCompanion,
        CancellationToken cancellationToken)
    {
        string destination = CreateDestination(
            sourcePath,
            reason,
            createdAt,
            liveCompanion);
        await _store.WriteLinesAsync(destination, lines, cancellationToken)
            .ConfigureAwait(false);
        return new BeatmapBackupArtifact(
            destination,
            sourcePath,
            reason,
            ContainsUnsavedState: true,
            createdAt);
    }

    private string CreateDestination(
        string sourcePath,
        BeatmapBackupReason reason,
        DateTimeOffset createdAt,
        bool liveCompanion)
    {
        string code = reason switch
        {
            BeatmapBackupReason.Automatic => "",
            BeatmapBackupReason.User => "UB",
            BeatmapBackupReason.Periodic => "PB",
            BeatmapBackupReason.RestoreSafety => "RU",
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };
        string separator = liveCompanion ? "_2_" : "__";
        string prefix = $"{createdAt:yyyy-MM-dd HH-mm-ss}_{code}";
        string fileName = _store.GetFileName(sourcePath);
        string destination = _store.Combine(
            _settings.BackupsPath,
            $"{prefix}{separator}{fileName}");
        for (int collision = 2; _store.FileExists(destination); collision++)
        {
            destination = _store.Combine(
                _settings.BackupsPath,
                $"{prefix}_C{collision}_{fileName}");
        }

        return destination;
    }

    private void ValidateRestore(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename)
    {
        if (!_store.FileExists(backupPath))
        {
            throw new FileNotFoundException(
                "The selected backup does not exist.",
                backupPath);
        }

        if (!_store.FileExists(destinationPath))
        {
            throw new FileNotFoundException(
                "The restore destination does not exist.",
                destinationPath);
        }

        if (allowDifferentFilename)
        {
            return;
        }

        BeatmapEditor2 backup = new(backupPath, _textFileStore);
        BeatmapEditor2 destination = new(destinationPath, _textFileStore);
        string backupFileName = backup.Beatmap.GetFileName();
        string destinationFileName = destination.Beatmap.GetFileName();
        if (!string.Equals(
                backupFileName,
                destinationFileName,
                StringComparison.Ordinal))
        {
            throw new BeatmapBackupIncompatibleException(
                backupFileName,
                destinationFileName);
        }
    }

    private async Task PruneAsync(
        IEnumerable<string> protectedPaths,
        CancellationToken cancellationToken)
    {
        HashSet<string> retained = new(
            protectedPaths,
            StringComparer.Ordinal);
        int limit = Math.Max(Math.Max(0, _settings.MaxBackupFiles), retained.Count);
        IReadOnlyList<StoredBeatmapBackup> backups = await _store
            .ListAsync(_settings.BackupsPath, cancellationToken)
            .ConfigureAwait(false);
        foreach (StoredBeatmapBackup backup in backups)
        {
            if (retained.Contains(backup.Path))
            {
                continue;
            }

            if (retained.Count < limit)
            {
                retained.Add(backup.Path);
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await _store.DeleteAsync(backup.Path, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void EnsureBackupDirectory()
    {
        if (!_store.DirectoryExists(_settings.BackupsPath))
        {
            throw new DirectoryNotFoundException(
                $"The configured backups folder '{_settings.BackupsPath}' does not exist.");
        }
    }

    private static string ComputeHash(IReadOnlyList<string> lines)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string line in lines)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(line));
            hash.AppendData([0x0A]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
