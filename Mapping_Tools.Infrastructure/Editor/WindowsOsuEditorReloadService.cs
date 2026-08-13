using System.Diagnostics;
using System.Runtime.InteropServices;
using Mapping_Tools.Application.BeatmapEditing;

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
    private readonly Func<Process?> _findProcess;

    /// <summary>
    /// Creates a reload adapter that discovers the active osu!stable process
    /// when a reload is requested.
    /// </summary>
    public WindowsOsuEditorReloadService()
        : this(OsuProcessDiscovery.FindStableProcess)
    {
    }

    internal WindowsOsuEditorReloadService(Func<Process?> findProcess)
    {
        _findProcess = findProcess ?? throw new ArgumentNullException(nameof(findProcess));
    }

    /// <inheritdoc/>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using Process? process = _findProcess();
        if (process is null || process.MainWindowHandle == IntPtr.Zero)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Reloading osu!'s editor is only supported on Windows.");
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
