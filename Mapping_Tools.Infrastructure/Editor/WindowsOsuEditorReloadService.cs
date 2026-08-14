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

        List<INPUT> inputs =
        [
            KeyboardInput(VirtualKeyControl, keyUp: false)
        ];
        for (int index = 0; index < 10; index++)
        {
            inputs.Add(KeyboardInput(VirtualKeyL, keyUp: false));
            inputs.Add(KeyboardInput(VirtualKeyL, keyUp: true));
        }

        inputs.Add(KeyboardInput(VirtualKeyControl, keyUp: true));
        SendKeyboardInput(inputs);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        SendKeyboardInput(
        [
            KeyboardInput(VirtualKeyEnter, keyUp: false),
            KeyboardInput(VirtualKeyEnter, keyUp: true)
        ]);
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

    private static void SendKeyboardInput(IReadOnlyList<INPUT> inputs)
    {
        INPUT[] nativeInputs = inputs.ToArray();
        uint sent = SendInput(
            (uint)nativeInputs.Length,
            nativeInputs,
            Marshal.SizeOf<INPUT>());
        if (sent != nativeInputs.Length)
        {
            throw new InvalidOperationException(
                $"Windows delivered {sent} of {nativeInputs.Length} reload key events.");
        }
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
        public KEYBDINPUT Keyboard;
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
}
