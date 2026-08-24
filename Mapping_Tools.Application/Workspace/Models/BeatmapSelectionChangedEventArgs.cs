namespace Mapping_Tools.Application.Workspace.Models;

/// <summary>
///     Describes a completed selection notification using an immutable path snapshot.
/// </summary>
/// <param name="Paths">The selected paths after the operation.</param>
/// <param name="Source">The action that produced the selection.</param>
public sealed record BeatmapSelectionChangedEventArgs(
    IReadOnlyList<string> Paths,
    BeatmapSelectionSource Source);

