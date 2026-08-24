using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Identifies a desktop window without exposing the operating system's native
///     handle representation to Application or Core.
/// </summary>
/// <param name="Value">The opaque window identifier supplied by Infrastructure.</param>
public readonly record struct PlatformWindowId(long Value)
{
    /// <summary>
    ///     Gets whether the identifier does not refer to a usable native window.
    /// </summary>
    public bool IsEmpty => Value == 0;
}

