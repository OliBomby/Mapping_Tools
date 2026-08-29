using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Contains one overlay primitive expressed in osu! editor coordinates.</summary>
/// <remarks>
///     <paramref name="Start" /> and <paramref name="End" /> are osu! coordinates.
///     For a point or circle, <paramref name="Start" /> is the centre. For a box,
///     they are the top-left and bottom-right corners. Radius is in osu! units for
///     circles and is a logical marker size for points; thickness remains a logical
///     presentation value owned by the renderer.
/// </remarks>
public sealed record GeometryDashboardOverlayShape(
    GeometryDashboardOverlayShapeKind Kind,
    Vector2 Start,
    Vector2 End,
    double Radius,
    RgbaColour Color,
    double Opacity,
    double Thickness,
    DashStylesEnum DashStyle);
