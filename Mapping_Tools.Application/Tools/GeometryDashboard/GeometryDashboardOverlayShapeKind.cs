using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>Identifies the primitive represented by one overlay shape.</summary>
public enum GeometryDashboardOverlayShapeKind
{
    /// <summary>An outline circle centered at <see cref="GeometryDashboardOverlayShape.Start" />.</summary>
    Point,

    /// <summary>A clipped line from <see cref="GeometryDashboardOverlayShape.Start" /> to End.</summary>
    Line,

    /// <summary>An outline circle centered at Start with Radius.</summary>
    Circle,
}

