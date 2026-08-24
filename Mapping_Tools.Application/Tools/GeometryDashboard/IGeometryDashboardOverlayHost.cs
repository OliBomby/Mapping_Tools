using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Owns the target-bound overlay window lifecycle used by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardOverlayHost : IDisposable
{
    /// <summary>Gets whether this host can create and control a native overlay.</summary>
    bool IsSupported { get; }

    /// <summary>Gets whether the overlay is currently visible.</summary>
    bool IsVisible { get; }

    /// <summary>Gets the target window currently followed by the overlay.</summary>
    PlatformWindowId? TargetWindow { get; }

    /// <summary>
    ///     Creates or retargets the transparent overlay to a top-level window.
    /// </summary>
    /// <param name="targetWindow">The window whose activation controls visibility.</param>
    void Initialize(PlatformWindowId targetWindow);

    /// <summary>Enables target activation tracking and overlay updates.</summary>
    void Enable();

    /// <summary>Disables tracking and hides the overlay.</summary>
    void Disable();

    /// <summary>
    ///     Updates the overlay bounds from physical screen pixels while preserving
    ///     the legacy DPI conversion and no-source fallback.
    /// </summary>
    /// <param name="physicalBounds">The editor rectangle in physical screen pixels.</param>
    /// <param name="dpiMultiplier">The device-to-logical scale used by the host window.</param>
    /// <param name="dpiSourceAvailable">Whether <paramref name="dpiMultiplier" /> came from a live window DPI source.</param>
    void Update(Box2 physicalBounds, Vector2 dpiMultiplier, bool dpiSourceAvailable);

    /// <summary>Changes the legacy debug border state.</summary>
    /// <param name="enabled">Whether a green-yellow border should be shown.</param>
    void SetBorder(bool enabled);

    /// <summary>
    ///     Replaces the geometry frame drawn by the click-through overlay.
    ///     Coordinates in the frame are physical screen pixels. The host subtracts
    ///     the bounds most recently supplied to <see cref="Update" /> before drawing.
    /// </summary>
    /// <param name="frame">The neutral geometry frame to draw.</param>
    void SetFrame(GeometryDashboardOverlayFrame frame);

    /// <summary>Requests a redraw of the platform overlay surface.</summary>
    void Invalidate();
}

