using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Contains platform-neutral presentation options for a Geometry Dashboard scene.</summary>
/// <param name="EditorBoxOffset">Per-edge osu! editor-space adjustment applied by Infrastructure.</param>
/// <param name="ShowDebugBorder">Whether Infrastructure should draw the diagnostic border.</param>
public sealed record GeometryDashboardOverlayOptions(
    Box2 EditorBoxOffset,
    bool ShowDebugBorder);
