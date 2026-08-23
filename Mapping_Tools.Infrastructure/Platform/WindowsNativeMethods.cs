using System.Runtime.InteropServices;
using System.Text;

namespace Mapping_Tools.Infrastructure.Platform;

internal static class WindowsNativeMethods
{
    internal const uint MONITOR_DEFAULT_TO_NEAREST = 2;
    internal const uint MONITOR_INFO_PRIMARY = 1;
    internal const uint DPI_TYPE_EFFECTIVE = 0;
    internal const int SHOW_HIDE = 0;
    internal const int SHOW_NO_ACTIVATE = 4;
    internal const uint SET_WINDOW_POS_NO_ACTIVATE = 0x0010;
    internal const uint SET_WINDOW_POS_NO_SEND_CHANGING = 0x0400;
    internal const uint WINDOW_MESSAGE_PAINT = 0x000F;
    internal const uint WINDOW_MESSAGE_ERASE_BACKGROUND = 0x0014;
    internal const uint WINDOW_MESSAGE_NC_DESTROY = 0x0082;
    internal const uint WINDOW_MESSAGE_NC_HIT_TEST = 0x0084;
    internal const nint HIT_TEST_TRANSPARENT = -1;
    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYBOARD_KEY_UP = 0x0002;

    internal static readonly nint TopMostWindow = new(-1);

    internal static int NativeInputSize => Marshal.SizeOf<Input>();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetCursorPos(int x, int y);

    internal static (uint Sent, int Error) SendKeyboardInput(
        byte virtualKey,
        bool keyUp)
    {
        Input[] nativeInputs =
        [
            new()
            {
                Type = INPUT_KEYBOARD,
                Data = new InputUnion
                {
                    Keyboard = new Keybdinput
                    {
                        VirtualKey = virtualKey,
                        Flags = keyUp ? KEYBOARD_KEY_UP : 0,
                    },
                },
            },
        ];
        uint sent = SendInput(1, nativeInputs, NativeInputSize);
        return (sent, sent == 1 ? 0 : Marshal.GetLastWin32Error());
    }

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint numberOfInputs,
        Input[] inputs,
        int inputSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clippingRectangle,
        EnumMonitorsCallback callback,
        nint data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(nint monitor, ref Monitorinfo monitorInfo);

    [DllImport("shcore.dll")]
    internal static extern int GetDpiForMonitor(
        nint monitor,
        uint dpiType,
        out uint dpiX,
        out uint dpiY);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsCallback callback, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint window, out Rect rectangle);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowTextLength(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(nint window, StringBuilder text, int capacity);

    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(nint window);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern nint GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern ushort RegisterClass(ref Wndclass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool InvalidateRect(nint window, nint rectangle, bool erase);

    [DllImport("user32.dll")]
    internal static extern nint DefWindowProc(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    internal static extern nint BeginPaint(nint window, out Paintstruct paint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EndPaint(nint window, ref Paintstruct paint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(nint window, out Rect rectangle);

    [DllImport("gdi32.dll")]
    internal static extern nint CreateSolidBrush(uint color);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint objectHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FrameRect(nint deviceContext, ref Rect rectangle, nint brush);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate bool EnumWindowsCallback(nint window, nint data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate bool EnumMonitorsCallback(nint monitor, nint hdc, ref Rect bounds, nint data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint WindowProcedure(nint window, uint message, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Monitorinfo
    {
        internal int Size;
        internal Rect Monitor;
        internal Rect Work;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Wndclass
    {
        internal uint Style;
        internal WindowProcedure? WindowProcedure;
        internal int ClassExtra;
        internal int WindowExtra;
        internal nint Instance;
        internal nint Icon;
        internal nint Cursor;
        internal nint Background;
        internal string? MenuName;
        internal string ClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Paintstruct
    {
        internal nint DeviceContext;
        internal int Erase;
        internal Rect Paint;
        internal int Restore;
        internal int IncUpdate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[]? Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        internal uint Type;
        internal InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] internal Mouseinput Mouse;

        [FieldOffset(0)] internal Keybdinput Keyboard;

        [FieldOffset(0)] internal Hardwareinput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Mouseinput
    {
        internal int Dx;
        internal int Dy;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Keybdinput
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Hardwareinput
    {
        internal uint Message;
        internal ushort ParameterLow;
        internal ushort ParameterHigh;
    }
}
