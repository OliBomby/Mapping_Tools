namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Identifies why a snapshot exists so filenames and future retention policies
///     can distinguish tool safety copies from explicit user actions.
/// </summary>
public enum BeatmapBackupReason
{
    /// <summary>
    ///     Protects the durable file immediately before a mapping operation overwrites it.
    /// </summary>
    Automatic,

    /// <summary>
    ///     Records a snapshot explicitly requested by the user.
    /// </summary>
    User,

    /// <summary>
    ///     Captures changed in-memory editor content during periodic monitoring.
    /// </summary>
    Periodic,

    /// <summary>
    ///     Preserves the current destination immediately before a restore or QuickUndo.
    /// </summary>
    RestoreSafety,
}

