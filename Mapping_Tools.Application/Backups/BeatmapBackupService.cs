using System.Security.Cryptography;
using System.Text;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

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
    private readonly ILogger<BeatmapBackupService> logger;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapBackupStore store;
    private readonly ITextFileStore textFileStore;
    private readonly TimeProvider timeProvider;
    private readonly IBeatmapDecoder beatmapDecoder;

    /// <summary>
    ///     Creates a process-lifetime backup coordinator whose serialization lock
    ///     prevents same-second requests from racing over legacy-compatible names.
    /// </summary>
    /// <param name="store">Physical copy, write, enumeration, and pruning operations.</param>
    /// <param name="textFileStore">Persistence used to validate beatmap metadata without direct filesystem access.</param>
    /// <param name="reloadService">The osu! refresh port used only after a successful restore.</param>
    /// <param name="settings">The current backup directory, enablement, and retention policy.</param>
    /// <param name="timeProvider">Supplies deterministic local timestamps for filenames and tests.</param>
    /// <param name="beatmapDecoder">Decodes beatmap metadata when validating a restore.</param>
    /// <param name="logger">Records backup and restore stages.</param>
    public BeatmapBackupService(
        IBeatmapBackupStore store,
        ITextFileStore textFileStore,
        IEditorReloadService reloadService,
        ApplicationSettings settings,
        TimeProvider timeProvider,
        IBeatmapDecoder beatmapDecoder,
        ILogger<BeatmapBackupService>? logger = null)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
        this.reloadService = reloadService ?? throw new ArgumentNullException(nameof(reloadService));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.beatmapDecoder = beatmapDecoder ?? throw new ArgumentNullException(nameof(beatmapDecoder));
        this.logger = logger ?? NullLogger<BeatmapBackupService>.Instance;
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

        logger.LogInformation("Creating {Reason} backup for live session {Path}", reason, session.Path);

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureBackupDirectory();
            var createdAt = timeProvider.GetLocalNow();
            var disk = await CopySourceAsync(
                    session.Path,
                    reason,
                    createdAt,
                    cancellationToken)
                .ConfigureAwait(false);
            List<BeatmapBackupArtifact> artifacts = [disk];

            if (session.Source == BeatmapEditingSource.LiveEditor && !HasSameContentsAsDisk(session.Path, session.InitialBeatmapText))
                // Save second copy with newest version if possible
                artifacts.Add(
                    await WriteSnapshotAsync(
                            session.Path,
                            session.InitialBeatmapText,
                            reason,
                            createdAt,
                            true,
                            cancellationToken)
                        .ConfigureAwait(false));

            await PruneAsync(
                    artifacts.Select(artifact => artifact.Path),
                    cancellationToken)
                .ConfigureAwait(false);
            logger.LogInformation("Created {Count} {Reason} backup files for {Path}: {Artifacts}",
                artifacts.Count, reason, session.Path, string.Join(" | ", artifacts.Select(artifact => artifact.Path)));
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

        string serializedText = session.GetSerializedText();
        IReadOnlyList<string> lines = ReadLines(serializedText);
        string hash = ComputeHash(lines);
        cancellationToken.ThrowIfCancellationRequested();

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (periodicHashes.TryGetValue(session.Path, out string? previous) && string.Equals(previous, hash, StringComparison.Ordinal))
                return null;

            EnsureBackupDirectory();
            var createdAt = timeProvider.GetLocalNow();
            // Save temp version
            var artifact = await WriteSnapshotAsync(
                    session.Path,
                    serializedText,
                    BeatmapBackupReason.Periodic,
                    createdAt,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);
            logger.LogInformation("Created periodic backup {BackupPath} for {Path}", artifact.Path, session.Path);
            periodicHashes[session.Path] = hash;
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

        logger.LogInformation("Restoring backup {BackupPath} to {DestinationPath}; different filename allowed {AllowDifferent}; reload {Reload}",
            backupPath, destinationPath, allowDifferentFilename, reloadEditor);
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
        logger.LogInformation("Restored backup {BackupPath} to {DestinationPath}; safety copy {SafetyPath}",
            backupPath, destinationPath, safetyArtifact.Path);

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
        var newest = backups.FirstOrDefault(backup => !IsPeriodicBackup(backup.Path));
        if (newest is null)
        {
            logger.LogInformation("QuickUndo found no eligible backup for {Path}", destinationPath);
            return null;
        }

        logger.LogInformation("QuickUndo selected backup {BackupPath} for {Path}", newest.Path, destinationPath);

        return await RestoreAsync(
                newest.Path,
                destinationPath,
                allowDifferentFilename,
                reloadEditor,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsPeriodicBackup(string path)
    {
        const int timestamp_length = 19;
        const string periodic_marker = "_PB_";
        string fileName = Path.GetFileName(path);
        return fileName.Length >= timestamp_length + periodic_marker.Length
               && fileName.AsSpan(timestamp_length).StartsWith(
                   periodic_marker,
                   StringComparison.Ordinal);
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
        logger.LogInformation("Creating {Reason} backup for {Count} paths: {Paths}; forced {Force}",
            reason, paths.Length, string.Join(" | ", paths), force);
        if (paths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException(
                ApplicationStrings.BeatmapBackup_EmptySourcePath,
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
            logger.LogInformation("Created {Count} {Reason} backups: {Artifacts}", artifacts.Count, reason,
                string.Join(" | ", artifacts.Select(artifact => artifact.Path)));
            return new BeatmapBackupResult(artifacts, false);
        }
        finally
        {
            operationLock.Release();
        }
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
        string serializedText,
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
        await store.WriteLinesAsync(destination, ReadLines(serializedText), cancellationToken)
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

        string backupFileName = beatmapDecoder
            .Decode(textFileStore.ReadAllText(backupPath))
            .GetFileName();
        string destinationFileName = beatmapDecoder
            .Decode(textFileStore.ReadAllText(destinationPath))
            .GetFileName();
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

    private bool HasSameContentsAsDisk(string path, string serializedText)
    {
        IReadOnlyList<string> diskLines = ReadLines(textFileStore.ReadAllText(path));
        IReadOnlyList<string> serializedLines = ReadLines(serializedText);
        return diskLines.SequenceEqual(serializedLines, StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> ReadLines(string text)
    {
        using StringReader reader = new(text);
        List<string> lines = [];
        string? line;
        while ((line = reader.ReadLine()) is not null)
            lines.Add(line);

        return lines;
    }
}
