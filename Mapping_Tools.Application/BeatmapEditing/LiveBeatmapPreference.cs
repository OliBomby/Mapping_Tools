using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Controls whether opening a beatmap may incorporate the unsaved state of
///     the map currently open in osu!.
/// </summary>
public enum LiveBeatmapPreference
{
    /// <summary>
    ///     Reads only the file named by the caller and never inspects osu!'s process memory.
    /// </summary>
    DiskOnly,

    /// <summary>
    ///     Uses matching live editor state when it is healthy, but keeps the
    ///     on-disk document when the editor is unavailable or cannot be read.
    /// </summary>
    PreferLive,

    /// <summary>
    ///     Requires healthy live state for the requested beatmap and reports a
    ///     failure instead of silently editing an older on-disk version.
    /// </summary>
    RequireLive,
}

