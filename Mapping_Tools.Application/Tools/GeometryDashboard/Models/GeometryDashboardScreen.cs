using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>
///     Describes one monitor using physical desktop pixels and its effective DPI.
/// </summary>
/// <param name="Id">The opaque monitor identifier.</param>
/// <param name="Bounds">The complete monitor rectangle, including negative virtual-screen coordinates.</param>
/// <param name="WorkingArea">The monitor rectangle excluding taskbars and other app bars.</param>
/// <param name="IsPrimary">Whether the monitor is the primary desktop monitor.</param>
/// <param name="DpiScale">The horizontal and vertical effective-DPI multipliers, relative to 96 DPI.</param>
/// <param name="DpiSourceAvailable">Whether Windows supplied the monitor DPI instead of the 96-DPI fallback.</param>
public sealed record GeometryDashboardScreen(
    long Id,
    Box2 Bounds,
    Box2 WorkingArea,
    bool IsPrimary,
    Vector2 DpiScale,
    bool DpiSourceAvailable);

