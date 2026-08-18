using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
/// Reads and moves global Windows input for Geometry Dashboard while keeping
/// the persisted WPF-compatible hotkey values in the neutral Core model.
/// </summary>
public sealed class WindowsGeometryDashboardInputService : IGeometryDashboardInputService
{
    private const int VirtualKeyLeftAlt = 0xA4;
    private const int VirtualKeyRightAlt = 0xA5;
    private const int VirtualKeyLeftControl = 0xA2;
    private const int VirtualKeyRightControl = 0xA3;
    private const int VirtualKeyLeftShift = 0xA0;
    private const int VirtualKeyRightShift = 0xA1;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;
    private const int VirtualKeyLeftButton = 0x01;
    private readonly Func<bool> _isWindows;

    /// <summary>Creates the adapter using the current platform guard.</summary>
    public WindowsGeometryDashboardInputService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardInputService(Func<bool> isWindows)
    {
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc/>
    public bool IsSupported => _isWindows();

    /// <inheritdoc/>
    public bool IsHotkeyDown(Hotkey? hotkey)
    {
        if (!_isWindows() || hotkey is null || hotkey.Key == 0)
        {
            return false;
        }

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

        return IsKeyDown(virtualKey) &&
               HasModifier(hotkey.Modifiers, 1, VirtualKeyLeftAlt, VirtualKeyRightAlt) &&
               HasModifier(hotkey.Modifiers, 2, VirtualKeyLeftControl, VirtualKeyRightControl) &&
               HasModifier(hotkey.Modifiers, 4, VirtualKeyLeftShift, VirtualKeyRightShift) &&
               HasModifier(hotkey.Modifiers, 8, VirtualKeyLeftWindows, VirtualKeyRightWindows);
    }

    /// <inheritdoc/>
    public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
    {
        if (!_isWindows())
        {
            return false;
        }

        return button switch
        {
            GeometryDashboardMouseButton.Left => IsKeyDown(VirtualKeyLeftButton),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    }

    /// <inheritdoc/>
    public bool TryGetCursorPosition(out Vector2 position)
    {
        position = Vector2.Zero;
        if (!_isWindows() || !WindowsNativeMethods.GetCursorPos(out WindowsNativeMethods.POINT point))
        {
            return false;
        }

        position = new Vector2(point.X, point.Y);
        return true;
    }

    /// <inheritdoc/>
    public bool TrySetCursorPosition(Vector2 position)
    {
        if (!_isWindows() ||
            !double.IsFinite(position.X) ||
            !double.IsFinite(position.Y) ||
            position.X < int.MinValue ||
            position.X > int.MaxValue ||
            position.Y < int.MinValue ||
            position.Y > int.MaxValue)
        {
            return false;
        }

        return WindowsNativeMethods.SetCursorPos(
            Convert.ToInt32(Math.Round(position.X)),
            Convert.ToInt32(Math.Round(position.Y)));
    }

    private static bool HasModifier(int modifiers, int flag, int leftKey, int rightKey)
    {
        return ((modifiers & flag) != 0) ==
               (IsKeyDown(leftKey) || IsKeyDown(rightKey));
    }

    private static bool IsKeyDown(int virtualKey) =>
        (WindowsNativeMethods.GetAsyncKeyState(virtualKey) & short.MinValue) != 0;
}
