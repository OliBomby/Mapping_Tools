using System.Runtime.InteropServices;
using Mapping_Tools.ApplicationServices.BeatmapEditing;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
/// Reproduces Mapping Tools' established Ctrl+L/Enter osu!stable reload
/// gesture using Win32 input rather than a Windows Forms dependency.
/// </summary>
public sealed class WindowsOsuEditorReloadService : IEditorReloadService
{
    private const byte VirtualKeyControl = 0x11;
    private const byte VirtualKeyL = 0x4C;
    private const byte VirtualKeyEnter = 0x0D;
    private const uint KeyUp = 0x0002;

    /// <inheritdoc/>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Reloading osu!'s editor is only supported on Windows.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        using System.Diagnostics.Process? process =
            OsuProcessDiscovery.FindStableProcess();
        if (process is null || process.MainWindowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "No running osu!stable window is available to reload.");
        }

        if (GetForegroundWindow() != process.MainWindowHandle)
        {
            if (!SetForegroundWindow(process.MainWindowHandle))
            {
                throw new InvalidOperationException(
                    "Windows did not allow Mapping Tools to focus osu!.");
            }

            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
        }

        KeyEvent(VirtualKeyControl, keyUp: false);
        for (int index = 0; index < 10; index++)
        {
            KeyEvent(VirtualKeyL, keyUp: false);
            KeyEvent(VirtualKeyL, keyUp: true);
        }

        KeyEvent(VirtualKeyControl, keyUp: true);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        KeyEvent(VirtualKeyEnter, keyUp: false);
        KeyEvent(VirtualKeyEnter, keyUp: true);
    }

    private static void KeyEvent(byte virtualKey, bool keyUp)
    {
        keybd_event(virtualKey, 0, keyUp ? KeyUp : 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern void keybd_event(
        byte virtualKey,
        byte scanCode,
        uint flags,
        UIntPtr extraInfo);
}
