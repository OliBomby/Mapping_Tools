using System.Security.Cryptography;
using System.Text;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Application.Backups;

/// <summary>
///     Enforces backup-before-overwrite ordering and preserves legacy-compatible
///     names and retention while leaving physical I/O to Infrastructure.
/// </summary>
public sealed class BeatmapBackupService : IBeatmapBackupService
{
    private readonly SemaphoreSlim operationLock = new(1, 1);

    private readonly Dictionary<string, string> periodicHashes =
        new(StringComparer.Ordinal);

    private readonly IEditorReloadService reloadService;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapBackupStore store;
    private readonly ITextFileStore textFileStore;
    private readonly TimeProvider timeProvider;

    /// <summary>
    ///     Creates a process-lifetime backup coordinator whose serialization lock
    ///     prevents same-second requests from racing over legacy-compatible names.
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
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
        this.reloadService = reloadService ?? throw new ArgumentNullException(nameof(reloadService));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<BeatmapBackupResult> CreateAsync(
        BeatmapEditingSession session,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!force && !settings.MakeBackups) return new BeatmapBackupResult([], true);

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureBackupDirectory();
            var createdAt = timeProvider.GetLocalNow();
            var disk = await CopySourceAsync(
                    session.Editor.Path,
                    reason,
                    createdAt,
                    cancellationToken)
                .ConfigureAwait(false);
            List<BeatmapBackupArtifact> artifacts = [disk];

            if (session.Source == BeatmapEditingSource.LiveEditor && !HasSameContentsAsDisk(session.Editor.Path, session.InitialBeatmapLines))
                // Save second copy with newest version if possible
                artifacts.Add(
                    await WriteSnapshotAsync(
                            session.Editor.Path,
                            session.InitialBeatmapLines,
                            reason,
                            createdAt,
                            true,
                            cancellationToken)
                        .ConfigureAwait(false));

            await PruneAsync(
                    artifacts.Select(artifact => artifact.Path),
                    cancellationToken)
                .ConfigureAwait(false);
            return new BeatmapBackupResult(artifacts, false);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!settings.MakePeriodicBackups) return null;

        IReadOnlyList<string> lines = session.Editor.Beatmap.GetLines();
        string hash = ComputeHash(lines);
        cancellationToken.ThrowIfCancellationRequested();

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (periodicHashes.TryGetValue(session.Editor.Path, out string? previous) && string.Equals(previous, hash, StringComparison.Ordinal))
                return null;

            EnsureBackupDirectory();
            var createdAt = timeProvider.GetLocalNow();
            // Save temp version
            var artifact = await WriteSnapshotAsync(
                    session.Editor.Path,
                    lines,
                    BeatmapBackupReason.Periodic,
                    createdAt,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);
            periodicHashes[session.Editor.Path] = hash;
            await PruneAsync([artifact.Path], cancellationToken)
                .ConfigureAwait(false);
            return artifact;
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
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
        var safety = await CreateFilesAsync(
                [destinationPath],
                BeatmapBackupReason.RestoreSafety,
                true,
                [backupPath],
                cancellationToken)
            .ConfigureAwait(false);
        var safetyArtifact = safety.Artifacts.Single();

        cancellationToken.ThrowIfCancellationRequested();
        await store.CopyAsync(
                backupPath,
                destinationPath,
                cancellationToken)
            .ConfigureAwait(false);

        if (reloadEditor)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await reloadService.ReloadAsync(cancellationToken).ConfigureAwait(false);
        }

        return new BeatmapRestoreResult(
            backupPath,
            destinationPath,
            safetyArtifact);
    }

    /// <inheritdoc />
    public async Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        EnsureBackupDirectory();
        var backups = await store
            .ListAsync(settings.BackupsPath, cancellationToken)
            .ConfigureAwait(false);
        var newest = backups.FirstOrDefault();
        if (newest is null) return null;

        return await RestoreAsync(
                newest.Path,
                destinationPath,
                allowDifferentFilename,
                reloadEditor,
                cancellationToken)
            .ConfigureAwait(false);
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
            throw new ArgumentException(
                "Backup source paths cannot contain an empty value.",
                nameof(sourcePaths));

        cancellationToken.ThrowIfCancellationRequested();
        if (!force && !settings.MakeBackups) return new BeatmapBackupResult([], true);

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureBackupDirectory();
            var createdAt = timeProvider.GetLocalNow();
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
            return new BeatmapBackupResult(artifacts, false);
        }
        finally
        {
            operationLock.Release();
        }
    }

    private bool HasSameContentsAsDisk(
        string path,
        IReadOnlyList<string> serializedLines)
    {
        var diskLines = textFileStore.ReadAllLines(path);
        return diskLines.SequenceEqual(serializedLines, StringComparer.Ordinal);
    }

    private async Task<BeatmapBackupArtifact> CopySourceAsync(
        string sourcePath,
        BeatmapBackupReason reason,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (!store.FileExists(sourcePath))
            throw new FileNotFoundException(
                "The beatmap selected for backup does not exist.",
                sourcePath);

        string destination = CreateDestination(
            sourcePath,
            reason,
            createdAt,
            false);
        // Save normal copy
        await store.CopyAsync(
                sourcePath,
                destination,
                cancellationToken)
            .ConfigureAwait(false);
        return new BeatmapBackupArtifact(
            destination,
            sourcePath,
            reason,
            false,
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
        await store.WriteLinesAsync(destination, lines, cancellationToken)
            .ConfigureAwait(false);
        return new BeatmapBackupArtifact(
            destination,
            sourcePath,
            reason,
            true,
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
            _ => throw new ArgumentOutOfRangeException(nameof(reason)),
        };
        string separator = liveCompanion ? "_2_" : "__";
        string prefix = $"{createdAt:yyyy-MM-dd HH-mm-ss}_{code}";
        string fileName = store.GetFileName(sourcePath);
        string destination = store.Combine(
            settings.BackupsPath,
            $"{prefix}{separator}{fileName}");
        for (int collision = 2; store.FileExists(destination); collision++)
            destination = store.Combine(
                settings.BackupsPath,
                $"{prefix}_C{collision}_{fileName}");

        return destination;
    }

    private void ValidateRestore(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename)
    {
        if (!store.FileExists(backupPath))
            throw new FileNotFoundException(
                "The selected backup does not exist.",
                backupPath);

        if (!store.FileExists(destinationPath))
            throw new FileNotFoundException(
                "The restore destination does not exist.",
                destinationPath);

        if (allowDifferentFilename) return;

        BeatmapEditor backup = new(backupPath, textFileStore);
        BeatmapEditor destination = new(destinationPath, textFileStore);
        string backupFileName = backup.Beatmap.GetFileName();
        string destinationFileName = destination.Beatmap.GetFileName();
        if (!string.Equals(
                backupFileName,
                destinationFileName,
                StringComparison.Ordinal))
            throw new BeatmapBackupIncompatibleException(
                backupFileName,
                destinationFileName);
    }

    private async Task PruneAsync(
        IEnumerable<string> protectedPaths,
        CancellationToken cancellationToken)
    {
        HashSet<string> retained = new(
            protectedPaths,
            StringComparer.Ordinal);
        int limit = Math.Max(Math.Max(0, settings.MaxBackupFiles), retained.Count);
        var backups = await store
            .ListAsync(settings.BackupsPath, cancellationToken)
            .ConfigureAwait(false);
        foreach (var backup in backups)
        {
            if (retained.Contains(backup.Path)) continue;

            if (retained.Count < limit)
            {
                retained.Add(backup.Path);
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await store.DeleteAsync(backup.Path, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void EnsureBackupDirectory()
    {
        if (!store.DirectoryExists(settings.BackupsPath))
            throw new DirectoryNotFoundException(
                $"The configured backups folder '{settings.BackupsPath}' does not exist.");
    }

    private static string ComputeHash(IReadOnlyList<string> lines)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string line in lines)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(line));
            hash.AppendData([0x0A]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
