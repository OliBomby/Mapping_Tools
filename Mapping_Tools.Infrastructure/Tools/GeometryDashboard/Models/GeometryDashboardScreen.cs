using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

/// <summary>Describes one monitor using physical desktop pixels and effective DPI.</summary>
/// <param name="Id">The opaque monitor identifier.</param>
/// <param name="Bounds">The complete monitor rectangle.</param>
/// <param name="WorkingArea">The monitor rectangle excluding app bars.</param>
/// <param name="IsPrimary">Whether this is the primary monitor.</param>
/// <param name="DpiScale">The effective-DPI multipliers for each axis.</param>
/// <param name="DpiSourceAvailable">Whether the platform supplied the DPI value.</param>
public sealed record GeometryDashboardScreen(
    long Id,
    Box2 Bounds,
    Box2 WorkingArea,
    bool IsPrimary,
    Vector2 DpiScale,
    bool DpiSourceAvailable);