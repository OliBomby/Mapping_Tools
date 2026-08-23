using System.Diagnostics;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Infrastructure.Platform;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Reproduces Mapping Tools' established Ctrl+L/Enter osu!stable reload
///     gesture using Win32 input rather than a Windows Forms dependency.
/// </summary>
public sealed class WindowsOsuEditorReloadService : IEditorReloadService
{
    private const byte VirtualKeyControl = 0x11;
    private const byte VirtualKeyL = 0x4C;
    private const byte VirtualKeyEnter = 0x0D;
    private readonly Func<Process?> _findProcess;

    /// <summary>
    ///     Creates a reload adapter that discovers the active osu!stable process
    ///     when a reload is requested.
    /// </summary>
    public WindowsOsuEditorReloadService()
        : this(OsuProcessDiscovery.FindStableProcess)
    {
    }

    internal WindowsOsuEditorReloadService(Func<Process?> findProcess)
    {
        _findProcess = findProcess ?? throw new ArgumentNullException(nameof(findProcess));
    }

    internal static int NativeInputSize => WindowsNativeMethods.NativeInputSize;

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = _findProcess();
        if (process is null || process.MainWindowHandle == IntPtr.Zero) return;

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "Reloading osu!'s editor is only supported on Windows.");

        if (WindowsNativeMethods.GetForegroundWindow() != process.MainWindowHandle)
        {
            if (!WindowsNativeMethods.SetForegroundWindow(process.MainWindowHandle))
                throw new InvalidOperationException(
                    "Windows did not allow Mapping Tools to focus osu!.");

            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
        }

        SendKeyboardInput(VirtualKeyControl, false);
        for (int index = 0; index < 10; index++)
        {
            SendKeyboardInput(VirtualKeyL, false);
            SendKeyboardInput(VirtualKeyL, true);
        }

        SendKeyboardInput(VirtualKeyControl, true);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        SendKeyboardInput(VirtualKeyEnter, false);
        SendKeyboardInput(VirtualKeyEnter, true);
    }

    private static void SendKeyboardInput(byte virtualKey, bool keyUp)
    {
        (uint sent, int error) = WindowsNativeMethods.SendKeyboardInput(virtualKey, keyUp);
        if (sent != 1)
            throw new InvalidOperationException(
                $"Windows delivered {sent} of 1 reload key events " + $"(Win32 error {error}).");

        // SendKeys' SendInput path yields between individual events. osu!'s editor
        // reliably observes the legacy sequence when it is delivered this way,
        // rather than as one large batch.
        Thread.Sleep(1);
    }
}
