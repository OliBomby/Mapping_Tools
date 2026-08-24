using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>Creates target-bound Geometry Dashboard overlay hosts.</summary>
public interface IGeometryDashboardOverlayHostFactory
{
    /// <summary>Creates a disposable overlay host, including an unavailable-platform no-op host.</summary>
    /// <returns>A host whose <see cref="IGeometryDashboardOverlayHost.IsSupported" /> reports platform availability.</returns>
    IGeometryDashboardOverlayHost Create();
}
