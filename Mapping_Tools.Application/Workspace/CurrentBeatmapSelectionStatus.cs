namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Distinguishes the outcomes of asking osu! for its current beatmap.
/// </summary>
public enum CurrentBeatmapSelectionStatus
{
    /// <summary>
    ///     A live path was found, exists locally, and became the sole selection.
    /// </summary>
    Selected,

    /// <summary>
    ///     The configured integration could not identify a current beatmap.
    /// </summary>
    Unavailable,

    /// <summary>
    ///     The integration returned a path that no longer exists on disk.
    /// </summary>
    FileMissing,
}

