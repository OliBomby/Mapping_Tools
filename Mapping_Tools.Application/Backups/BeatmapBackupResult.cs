using Mapping_Tools.Application.BeatmapEditing;

namespace Mapping_Tools.Application.Backups;

/// <summary>
///     Collects every file produced for a request that may protect several maps
///     or both the durable and unsaved versions of one map.
/// </summary>
/// <param name="Artifacts">Backups in source order, with a live companion immediately after its disk copy.</param>
/// <param name="SkippedByPreference">
///     Whether automatic backups were disabled and the caller did not force the request.
/// </param>
public sealed record BeatmapBackupResult(
    IReadOnlyList<BeatmapBackupArtifact> Artifacts,
    bool SkippedByPreference);

