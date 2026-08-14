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
    private const uint InputKeyboard = 1;
    private const uint KeyboardKeyUp = 0x0002;
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

    internal static int NativeInputSize => Marshal.SizeOf<INPUT>();

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

        SendKeyboardInput(VirtualKeyControl, keyUp: false);
        for (int index = 0; index < 10; index++)
        {
            SendKeyboardInput(VirtualKeyL, keyUp: false);
            SendKeyboardInput(VirtualKeyL, keyUp: true);
        }

        SendKeyboardInput(VirtualKeyControl, keyUp: true);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        SendKeyboardInput(VirtualKeyEnter, keyUp: false);
        SendKeyboardInput(VirtualKeyEnter, keyUp: true);
    }

    private static INPUT KeyboardInput(byte virtualKey, bool keyUp)
    {
        return new INPUT
        {
            Type = InputKeyboard,
            Data = new INPUT_UNION
            {
                Keyboard = new KEYBDINPUT
                {
                    VirtualKey = virtualKey,
                    Flags = keyUp ? KeyboardKeyUp : 0
                }
            }
        };
    }

    private static void SendKeyboardInput(byte virtualKey, bool keyUp)
    {
        INPUT[] nativeInputs = [KeyboardInput(virtualKey, keyUp)];
        uint sent = SendInput(
            1,
            nativeInputs,
            NativeInputSize);
        if (sent != 1)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Windows delivered {sent} of 1 reload key events " +
                $"(Win32 error {error}).");
        }

        // SendKeys' SendInput path yields between individual events. osu!'s editor
        // reliably observes the legacy sequence when it is delivered this way,
        // rather than as one large batch.
        Thread.Sleep(1);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint numberOfInputs,
        INPUT[] inputs,
        int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public INPUT_UNION Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_UNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;

        [FieldOffset(0)]
        public HARDWAREINPUT Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }
}
