namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Carries only the physical metadata required to order and prune retained backups.
/// </summary>
/// <param name="Path">The complete file path.</param>
/// <param name="CreatedAt">The filesystem creation timestamp used by legacy ordering.</param>
public sealed record StoredBeatmapBackup(string Path, DateTimeOffset CreatedAt);

