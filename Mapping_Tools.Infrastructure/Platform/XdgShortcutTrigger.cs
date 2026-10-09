using System.Globalization;
using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Infrastructure.Platform;

internal static class XdgShortcutTrigger
{
    private static readonly Dictionary<string, int> keys = CreateKeys();

    internal static HotkeySettings ParseDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return new HotkeySettings(0, 0);

        string trigger = description.Split(", ", 2)[0].Trim();
        if (trigger.StartsWith("Press ", StringComparison.Ordinal))
            trigger = trigger[6..].Split(" or ", 2)[0];
        int modifiers = 0;
        int acceleratorStart = trigger.IndexOf('<');
        if (acceleratorStart >= 0)
        {
            // GNOME descriptions wrap GTK accelerators in localized prose.
            trigger = trigger[acceleratorStart..];
            while (trigger.StartsWith('<'))
            {
                int end = trigger.IndexOf('>');
                if (end < 0) throw new FormatException($"Unrecognized desktop shortcut: {description}");
                modifiers |= ParseModifier(trigger[1..end]);
                trigger = trigger[(end + 1)..];
            }

            trigger = trigger.Split(' ', 2)[0];
        }
        else
        {
            // KDE uses Qt's native key-sequence notation.
            while (trigger.IndexOf('+') is > 0 and var separator)
            {
                string modifier = trigger[..separator].Trim();
                if (modifier.Equals("Num", StringComparison.OrdinalIgnoreCase))
                {
                    trigger = "KP_" + trigger[(separator + 1)..].Trim();
                    break;
                }

                modifiers |= ParseModifier(modifier);
                trigger = trigger[(separator + 1)..].Trim();
            }
        }

        if (!keys.TryGetValue(trigger, out int key))
            throw new FormatException($"Unrecognized desktop shortcut: {description}");

        return new HotkeySettings(key, modifiers);
    }

    private static int ParseModifier(string modifier)
    {
        return modifier.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" or "PRIMARY" or "STRG" => 2,
            "ALT" or "MOD1" => 1,
            "SHIFT" or "UMSCHALT" or "MAJ" or "MAYÚS" => 4,
            "META" or "SUPER" or "LOGO" or "WIN" or "MOD4" => 8,
            _ => throw new FormatException($"Unrecognized desktop shortcut modifier: {modifier}"),
        };
    }

    private static Dictionary<string, int> CreateKeys()
    {
        Dictionary<string, int> result = new(StringComparer.OrdinalIgnoreCase);
        for (int key = 1; key <= 154; key++)
        {
            if (GetKeyName(key) is { } name) result[name] = key;
        }

        result["Esc"] = 13;
        result["Enter"] = 6;
        result["PgUp"] = 19;
        result["PgDown"] = 20;
        result["Ins"] = 31;
        result["Del"] = 32;
        result["CapsLock"] = 8;
        result["NumLock"] = 114;
        result["ScrollLock"] = 115;
        result["KP_*"] = 84;
        result["KP_+"] = 85;
        result["KP_,"] = 86;
        result["KP_-"] = 87;
        result["KP_."] = 88;
        result["KP_/"] = 89;
        result[";"] = 140;
        result["="] = result["+"] = 141;
        result[","] = 142;
        result["-"] = 143;
        result["."] = 144;
        result["/"] = 145;
        result["`"] = 146;
        result["["] = 149;
        result["\\"] = 150;
        result["]"] = 151;
        result["'"] = 152;
        return result;
    }

    internal static string? Convert(HotkeySettings hotkey)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hotkey.Modifiers);
        if ((hotkey.Modifiers & ~15) != 0)
            throw new ArgumentOutOfRangeException(nameof(hotkey), "Only Alt, Control, Shift, and Meta modifiers are supported.");

        string? key = GetKeyName(hotkey.Key);
        if (key is null) return null;

        List<string> parts = [];
        if ((hotkey.Modifiers & 2) != 0) parts.Add("CTRL");
        if ((hotkey.Modifiers & 1) != 0) parts.Add("ALT");
        if ((hotkey.Modifiers & 4) != 0) parts.Add("SHIFT");
        if ((hotkey.Modifiers & 8) != 0) parts.Add("LOGO");
        parts.Add(key);
        return string.Join('+', parts);
    }

    private static string? GetKeyName(int key)
    {
        if (key is >= 34 and <= 43) return (key - 34).ToString(CultureInfo.InvariantCulture);
        if (key is >= 44 and <= 69) return ((char)('a' + key - 44)).ToString();
        if (key is >= 74 and <= 83) return "KP_" + (key - 74).ToString(CultureInfo.InvariantCulture);
        if (key is >= 90 and <= 113) return "F" + (key - 89).ToString(CultureInfo.InvariantCulture);

        return key switch
        {
            1 => "Cancel",
            2 => "BackSpace",
            3 => "Tab",
            4 or 6 => "Return",
            5 => "Clear",
            7 => "Pause",
            8 => "Caps_Lock",
            9 => "Kana_Lock",
            12 => "Hangul_Hanja",
            13 => "Escape",
            14 => "Henkan",
            15 => "Muhenkan",
            18 => "space",
            19 => "Page_Up",
            20 => "Page_Down",
            21 => "End",
            22 => "Home",
            23 => "Left",
            24 => "Up",
            25 => "Right",
            26 => "Down",
            28 or 30 => "Print",
            31 => "Insert",
            32 => "Delete",
            33 => "Help",
            70 => "Super_L",
            71 => "Super_R",
            72 => "Menu",
            73 => "XF86Sleep",
            84 => "KP_Multiply",
            85 => "KP_Add",
            86 => "KP_Separator",
            87 => "KP_Subtract",
            88 => "KP_Decimal",
            89 => "KP_Divide",
            114 => "Num_Lock",
            115 => "Scroll_Lock",
            116 => "Shift_L",
            117 => "Shift_R",
            118 => "Control_L",
            119 => "Control_R",
            120 => "Alt_L",
            121 => "Alt_R",
            122 => "XF86Back",
            123 => "XF86Forward",
            124 => "XF86Refresh",
            125 => "XF86Stop",
            126 => "XF86Search",
            127 => "XF86Favorites",
            128 => "XF86HomePage",
            129 => "XF86AudioMute",
            130 => "XF86AudioLowerVolume",
            131 => "XF86AudioRaiseVolume",
            132 => "XF86AudioNext",
            133 => "XF86AudioPrev",
            134 => "XF86AudioPlay",
            135 => "XF86AudioStop",
            136 => "XF86Mail",
            137 => "XF86AudioMedia",
            138 => "XF86Launch0",
            139 => "XF86Launch1",
            140 => "semicolon",
            141 => "equal",
            142 => "comma",
            143 => "minus",
            144 => "period",
            145 => "slash",
            146 => "grave",
            147 or 154 => "section",
            148 => "yen",
            149 => "bracketleft",
            150 => "backslash",
            151 => "bracketright",
            152 => "apostrophe",
            // Let the desktop choose keys without a portable keysym name.
            _ => null,
        };
    }
}
