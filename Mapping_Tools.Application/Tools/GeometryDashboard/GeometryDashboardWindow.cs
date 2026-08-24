using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Describes a top-level window in physical desktop pixels.
/// </summary>
/// <param name="Id">The opaque native window identifier.</param>
/// <param name="ProcessId">The owning process identifier, when it was available.</param>
/// <param name="Title">The current window title.</param>
/// <param name="Bounds">The screen-space rectangle, including native window chrome.</param>
/// <param name="IsVisible">Whether Windows reports the window as visible.</param>
/// <param name="IsActivated">Whether the window is the current foreground window.</param>
/// <param name="DpiScale">The horizontal and vertical effective-DPI multipliers for the window.</param>
/// <param name="DpiSourceAvailable">Whether the window DPI was supplied by Windows.</param>
public sealed record GeometryDashboardWindow(
    PlatformWindowId Id,
    long ProcessId,
    string Title,
    Box2 Bounds,
    bool IsVisible,
    bool IsActivated,
    Vector2 DpiScale,
    bool DpiSourceAvailable);

