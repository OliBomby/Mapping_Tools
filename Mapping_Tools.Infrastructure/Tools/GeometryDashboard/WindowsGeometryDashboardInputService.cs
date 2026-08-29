using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Infrastructure.Platform;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Reads and moves global Windows input for Geometry Dashboard while keeping
///     the persisted WPF-compatible hotkey values in the neutral Core model.
/// </summary>
public sealed class WindowsGeometryDashboardInputService : IGeometryDashboardInputService
{
    private const int virtual_key_left_alt = 0xA4;
    private const int virtual_key_right_alt = 0xA5;
    private const int virtual_key_left_control = 0xA2;
    private const int virtual_key_right_control = 0xA3;
    private const int virtual_key_left_shift = 0xA0;
    private const int virtual_key_right_shift = 0xA1;
    private const int virtual_key_left_windows = 0x5B;
    private const int virtual_key_right_windows = 0x5C;
    private const int virtual_key_left_button = 0x01;
    private readonly WindowsGeometryDashboardCoordinateContext? coordinates;
    private readonly Func<bool> isWindows;

    /// <summary>Creates the input adapter using the shared live coordinate context.</summary>
    /// <param name="coordinates">Resolves the latest osu! editor transform.</param>
    public WindowsGeometryDashboardInputService(WindowsGeometryDashboardCoordinateContext coordinates)
        : this(coordinates, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardInputService(Func<bool> isWindows)
        : this(null, isWindows)
    {
    }

    private WindowsGeometryDashboardInputService(
        WindowsGeometryDashboardCoordinateContext? coordinates,
        Func<bool> isWindows)
    {
        this.coordinates = coordinates;
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public bool IsSupported => isWindows();

    /// <inheritdoc />
    public bool IsHotkeyDown(Hotkey? hotkey)
    {
        if (!isWindows() || hotkey is null || hotkey.Key == 0) return false;

        int virtualKey;
        try
        {
            virtualKey = WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(
                hotkey.Key);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return IsKeyDown(virtualKey)
               && HasModifier(hotkey.Modifiers, 1, virtual_key_left_alt, virtual_key_right_alt)
               && HasModifier(hotkey.Modifiers, 2, virtual_key_left_control, virtual_key_right_control)
               && HasModifier(hotkey.Modifiers, 4, virtual_key_left_shift, virtual_key_right_shift)
               && HasModifier(hotkey.Modifiers, 8, virtual_key_left_windows, virtual_key_right_windows);
    }

    /// <inheritdoc />
    public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
    {
        if (!isWindows()) return false;

        return button switch
        {
            GeometryDashboardMouseButton.Left => IsKeyDown(virtual_key_left_button),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
        };
    }

    /// <inheritdoc />
    public bool TryGetCursorPosition(out Vector2 position)
    {
        position = Vector2.Zero;
        if (!isWindows()
            || coordinates is null
            || !coordinates.TryRefresh(out var snapshot)
            || !WindowsNativeMethods.GetCursorPos(out var point))
            return false;

        position = snapshot.Transform.ScreenToEditorCoordinate(new Vector2(point.X, point.Y));
        return true;
    }

    /// <inheritdoc />
    public bool TrySetCursorPosition(Vector2 position)
    {
        if (!isWindows()
            || coordinates is null
            || !double.IsFinite(position.X)
            || !double.IsFinite(position.Y)
            || position.X < int.MinValue
            || position.X > int.MaxValue
            || position.Y < int.MinValue
            || position.Y > int.MaxValue)
            return false;

        if (!coordinates.TryRefresh(out var snapshot)) return false;

        Vector2 screen = snapshot.Transform.EditorToScreenCoordinate(position);
        return double.IsFinite(screen.X)
               && double.IsFinite(screen.Y)
               && screen.X >= int.MinValue
               && screen.X <= int.MaxValue
               && screen.Y >= int.MinValue
               && screen.Y <= int.MaxValue
               && WindowsNativeMethods.SetCursorPos(
                   Convert.ToInt32(Math.Round(screen.X)),
                   Convert.ToInt32(Math.Round(screen.Y)));
    }

    private static bool HasModifier(int modifiers, int flag, int leftKey, int rightKey)
    {
        return (modifiers & flag) != 0 == (IsKeyDown(leftKey) || IsKeyDown(rightKey));
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (WindowsNativeMethods.GetAsyncKeyState(virtualKey) & short.MinValue) != 0;
    }
}
