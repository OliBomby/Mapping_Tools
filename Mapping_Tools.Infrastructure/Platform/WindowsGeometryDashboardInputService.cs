using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Infrastructure.Platform;

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
    private readonly Func<bool> isWindows;

    /// <summary>Creates the adapter using the current platform guard.</summary>
    public WindowsGeometryDashboardInputService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardInputService(Func<bool> isWindows)
    {
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
        if (!isWindows() || !WindowsNativeMethods.GetCursorPos(out var point)) return false;

        position = new Vector2(point.X, point.Y);
        return true;
    }

    /// <inheritdoc />
    public bool TrySetCursorPosition(Vector2 position)
    {
        if (!isWindows()
            || !double.IsFinite(position.X)
            || !double.IsFinite(position.Y)
            || position.X < int.MinValue
            || position.X > int.MaxValue
            || position.Y < int.MinValue
            || position.Y > int.MaxValue)
            return false;

        return WindowsNativeMethods.SetCursorPos(
            Convert.ToInt32(Math.Round(position.X)),
            Convert.ToInt32(Math.Round(position.Y)));
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
