using System.Diagnostics;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Platform.GeometryDashboard;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Reproduces Mapping Tools' established Ctrl+L/Enter osu!stable reload
///     gesture using Win32 input rather than a Windows Forms dependency.
/// </summary>
public sealed class WindowsOsuEditorReloadService : IEditorReloadService
{
    private const byte virtual_key_control = 0x11;
    private const byte virtual_key_l = 0x4C;
    private const byte virtual_key_enter = 0x0D;
    private readonly Func<Process?> findProcess;

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
        this.findProcess = findProcess ?? throw new ArgumentNullException(nameof(findProcess));
    }

    internal static int NativeInputSize => WindowsNativeMethods.NativeInputSize;

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = findProcess();
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

        SendKeyboardInput(virtual_key_control, false);
        for (int index = 0; index < 10; index++)
        {
            SendKeyboardInput(virtual_key_l, false);
            SendKeyboardInput(virtual_key_l, true);
        }

        SendKeyboardInput(virtual_key_control, true);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        SendKeyboardInput(virtual_key_enter, false);
        SendKeyboardInput(virtual_key_enter, true);
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
