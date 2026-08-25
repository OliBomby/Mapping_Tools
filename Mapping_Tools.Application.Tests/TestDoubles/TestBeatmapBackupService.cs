using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing.Models;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal sealed class TestBeatmapBackupService : IBeatmapBackupService
{
    public List<(IReadOnlyList<string> Paths, BeatmapBackupReason Reason, bool Force)> CreateRequests { get; } = [];

    public Task<BeatmapBackupResult> CreateAsync(
        IEnumerable<string> sourcePaths,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        string[] paths = sourcePaths.ToArray();
        CreateRequests.Add((paths, reason, force));
        IReadOnlyList<BeatmapBackupArtifact> artifacts = paths
            .Select(path => new BeatmapBackupArtifact(
                path + ".backup",
                path,
                reason,
                false,
                DateTimeOffset.UnixEpoch))
            .ToArray();
        return Task.FromResult(new BeatmapBackupResult(artifacts, false));
    }

    public Task<BeatmapBackupResult> CreateAsync(
        BeatmapEditingSession session,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateRequests.Add(([session.Editor.Path], reason, force));
        return Task.FromResult(
            new BeatmapBackupResult(
                [
                    new BeatmapBackupArtifact(
                        session.Editor.Path + ".backup",
                        session.Editor.Path,
                        reason,
                        session.Source == BeatmapEditingSource.LiveEditor,
                        DateTimeOffset.UnixEpoch),
                ],
                false));
    }

    public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<BeatmapRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<BeatmapRestoreResult?>(null);
    }
}
