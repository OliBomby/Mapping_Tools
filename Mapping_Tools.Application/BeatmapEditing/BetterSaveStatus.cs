using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Distinguishes a completed BetterSave from missing editor state and a captured failure.
/// </summary>
public enum BetterSaveStatus
{
    /// <summary>The current live editor document was backed up and saved.</summary>
    Saved,

    /// <summary>osu! did not expose a current beatmap path.</summary>
    NoCurrentBeatmap,

    /// <summary>Opening live state, creating the backup, or saving failed.</summary>
    Failed,
}

