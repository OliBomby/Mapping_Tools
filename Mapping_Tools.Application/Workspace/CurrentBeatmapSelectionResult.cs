namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Reports live-selection status together with the candidate path when one was available.
/// </summary>
/// <param name="Status">Whether selection succeeded or why it was left unchanged.</param>
/// <param name="Path">
///     The locator's candidate path, or <see langword="null" /> when lookup was unavailable.
/// </param>
public sealed record CurrentBeatmapSelectionResult(
    CurrentBeatmapSelectionStatus Status,
    string? Path);
