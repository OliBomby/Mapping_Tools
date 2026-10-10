using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>Renders a Geometry Dashboard scene in osu! editor coordinates.</summary>
public interface IGeometryDashboardOverlayService : IDisposable
{
    /// <summary>Gets whether the current platform can display the overlay.</summary>
    bool IsSupported { get; }

    /// <summary>Gets whether the overlay is currently visible over osu!.</summary>
    bool IsVisible { get; }

    /// <summary>Gets the most recent osu! configuration status reported by Infrastructure.</summary>
    string? ConfigurationStatus { get; }

    /// <summary>
    ///     Replaces the scene and displays it over the current osu! editor.
    ///     Infrastructure resolves the current window, monitor, DPI, and osu!
    ///     configuration for every update, so callers never provide screen-space data.
    /// </summary>
    /// <param name="scene">Geometry primitives expressed in osu! editor coordinates.</param>
    /// <param name="options">Presentation options that do not contain platform coordinates.</param>
    void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options);

    /// <summary>Hides the overlay while retaining its reusable native resources.</summary>
    void Hide();
}
