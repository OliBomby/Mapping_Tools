using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Contains one physical-pixel overlay primitive and its appearance.</summary>
public sealed record GeometryDashboardOverlayShape(
    GeometryDashboardOverlayShapeKind Kind,
    Vector2 Start,
    Vector2 End,
    double Radius,
    RgbaColour Color,
    double Opacity,
    double Thickness,
    DashStylesEnum DashStyle);

