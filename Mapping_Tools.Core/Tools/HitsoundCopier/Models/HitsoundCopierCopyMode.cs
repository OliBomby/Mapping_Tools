namespace Mapping_Tools.Core.Tools.HitsoundCopier.Models;

/// <summary>Determines which target hitsound and timing values are replaced.</summary>
/// <remarks>The numeric values are retained for compatibility with legacy project files.</remarks>
public enum HitsoundCopierCopyMode
{
    /// <summary>Replaces all target values covered by the selected copy settings.</summary>
    OverwriteEverything = 0,

    /// <summary>Replaces only values explicitly defined by the source beatmap.</summary>
    OverwriteOnlyDefined = 1,
}
