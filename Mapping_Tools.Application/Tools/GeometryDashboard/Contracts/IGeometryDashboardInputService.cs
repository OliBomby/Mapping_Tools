using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Provides global keyboard, mouse-button, and absolute-cursor state.
/// </summary>
public interface IGeometryDashboardInputService
{
    /// <summary>Gets whether the current platform exposes the required global input APIs.</summary>
    bool IsSupported { get; }

    /// <summary>
    ///     Tests a persisted activation or editing hotkey using exact modifier-state matching.
    /// </summary>
    /// <param name="hotkey">The legacy-compatible key and modifier values.</param>
    /// <returns><see langword="true" /> only while the complete combination is held.</returns>
    bool IsHotkeyDown(Hotkey? hotkey);

    /// <summary>
    ///     Tests whether a pointer button is currently held.
    /// </summary>
    /// <param name="button">The button to inspect.</param>
    /// <returns><see langword="true" /> while the button is held.</returns>
    bool IsMouseButtonDown(GeometryDashboardMouseButton button);

    /// <summary>
    ///     Attempts to read the absolute cursor position in physical desktop pixels.
    /// </summary>
    /// <param name="position">Receives the cursor position when the read succeeds.</param>
    /// <returns><see langword="true" /> when Windows supplied a position.</returns>
    bool TryGetCursorPosition(out Vector2 position);

    /// <summary>
    ///     Attempts to move the absolute cursor in physical desktop pixels.
    /// </summary>
    /// <param name="position">The destination; fractional values are rounded like the legacy adapter.</param>
    /// <returns><see langword="true" /> when Windows accepted the move.</returns>
    bool TrySetCursorPosition(Vector2 position);
}

