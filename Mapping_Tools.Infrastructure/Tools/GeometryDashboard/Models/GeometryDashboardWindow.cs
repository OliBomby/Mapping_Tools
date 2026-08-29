using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

/// <summary>Describes a top-level window in physical desktop pixels.</summary>
/// <param name="Id">The opaque native window identifier.</param>
/// <param name="ProcessId">The owning operating-system process identifier.</param>
/// <param name="Title">The current window title.</param>
/// <param name="Bounds">The complete window rectangle in physical desktop pixels.</param>
/// <param name="IsVisible">Whether the platform reports the window as visible.</param>
/// <param name="IsActivated">Whether the window is currently foreground.</param>
/// <param name="DpiScale">The effective-DPI multipliers for each axis.</param>
/// <param name="DpiSourceAvailable">Whether the platform supplied the DPI value.</param>
public sealed record GeometryDashboardWindow(
    PlatformWindowId Id,
    long ProcessId,
    string Title,
    Box2 Bounds,
    bool IsVisible,
    bool IsActivated,
    Vector2 DpiScale,
    bool DpiSourceAvailable);