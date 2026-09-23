namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Converts Avalonia key values to Windows virtual-key
///     values for the Geometry Dashboard's native input adapter.
/// </summary>
internal static class WindowsKeyCodeConverter
{
    internal static int ConvertKeyToVirtualKey(int key)
    {
        if (key is >= 18 and <= 43) return key + 14; // Avalonia Space through D9 follow the Win32 sequence.

        if (key is >= 44 and <= 72) return key + 21; // Avalonia A through Apps follow the Win32 sequence.

        if (key is >= 74 and <= 83) return key + 22; // Avalonia NumPad0-NumPad9 to Win32 numpad keys.

        if (key is >= 90 and <= 113) return key + 22; // Avalonia F1-F24 to Win32 function keys.

        if (key is >= 116 and <= 121) return key + 44; // Avalonia left/right modifier keys.

        if (key is >= 122 and <= 139) return key + 44; // Browser, media, and launch keys.

        if (key is >= 140 and <= 148) return key + 46; // OEM1 through ABNT C2.

        if (key is >= 149 and <= 153) return key + 70; // OEM4 through OEM8.

        if (key is >= 157 and <= 171) return key + 83; // IME DBE keys through OEM Clear.

        return key switch
        {
            1 => 0x03, // Cancel
            2 => 0x08, // Backspace
            3 => 0x09, // Tab
            4 => 0x0A, // Line feed
            5 => 0x0C, // Clear
            6 => 0x0D, // Enter
            7 => 0x13, // Pause
            8 => 0x14, // Caps lock
            9 => 0x15, // Kana mode
            10 => 0x17, // Junja mode
            11 => 0x18, // Final mode
            12 => 0x19, // Hanja mode
            13 => 0x1B, // Escape
            14 => 0x1C, // IME convert
            15 => 0x1D, // IME non-convert
            16 => 0x1E, // IME accept
            17 => 0x1F, // IME mode change
            73 => 0x5F, // Sleep
            84 => 0x6A, // Multiply
            85 => 0x6B, // Add
            86 => 0x6C, // Separator
            87 => 0x6D, // Subtract
            88 => 0x6E, // Decimal
            89 => 0x6F, // Divide
            114 => 0x90, // Num Lock
            115 => 0x91, // Scroll Lock
            155 => 0xE5, // IME processed
            154 => 0xE2, // OEM 102
            _ => throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "The Avalonia key is not supported by the Windows input adapter."),
        };
    }
}
