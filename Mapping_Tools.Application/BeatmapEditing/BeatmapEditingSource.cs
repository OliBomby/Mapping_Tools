using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Describes which source supplied the mutable beatmap returned by an editing session.
/// </summary>
public enum BeatmapEditingSource
{
    /// <summary>
    ///     The session contains exactly the version parsed from disk.
    /// </summary>
    Disk,

    /// <summary>
    ///     The disk document was updated with unsaved timing, object, bookmark,
    ///     and difficulty state read from osu!.
    /// </summary>
    LiveEditor,
}

