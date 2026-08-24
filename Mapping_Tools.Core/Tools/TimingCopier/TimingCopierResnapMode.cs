using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.TimingCopier;

/// <summary>
///     Identifies how Timing Copier moves target map content after copying timing.
/// </summary>
public enum TimingCopierResnapMode
{
    /// <summary>
    ///     Moves markers so their beat distances remain stable, then snaps them.
    /// </summary>
    PreserveBeatSpacing,

    /// <summary>
    ///     Snaps movable map content to the copied timing without preserving beat distances.
    /// </summary>
    Resnap,

    /// <summary>
    ///     Replaces timing while leaving map content at its existing timestamps.
    /// </summary>
    KeepObjectsFixed,
}

