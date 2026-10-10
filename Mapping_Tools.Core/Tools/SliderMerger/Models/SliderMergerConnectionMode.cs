namespace Mapping_Tools.Core.Tools.SliderMerger.Models;

/// <summary>
///     Describes how two converted slider paths are joined.
/// </summary>
public enum SliderMergerConnectionMode
{
    /// <summary>Moves the next path so its start meets the previous path's end.</summary>
    Move,

    /// <summary>Adds a straight Bézier-encoded gap between the two paths.</summary>
    Linear,
}
