using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
/// Captures one Avalonia key gesture while persisting the legacy WPF numeric key format.
/// </summary>
public sealed class HotkeyEditor : TextBox
{
    /// <summary>Identifies the two-way bindable legacy-compatible hotkey value.</summary>
    public static readonly StyledProperty<HotkeySettings?> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyEditor, HotkeySettings?>(
            nameof(Hotkey),
            defaultBindingMode: BindingMode.TwoWay);

    static HotkeyEditor()
    {
        HotkeyProperty.Changed.AddClassHandler<HotkeyEditor>(
            static (editor, _) => editor.UpdateText());
    }

    /// <summary>Creates a read-only field whose keyboard input edits only the hotkey value.</summary>
    public HotkeyEditor()
    {
        IsReadOnly = true;
        IsUndoEnabled = false;
        HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
        UpdateText();
    }

    /// <summary>
    /// Gets or sets the persisted key and legacy Alt, Control, Shift, and Windows modifier bits.
    /// </summary>
    public HotkeySettings? Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    /// <summary>
    /// Captures supported keys, ignores modifier-only presses, and clears on unmodified Delete,
    /// Backspace, or Escape.
    /// </summary>
    /// <param name="eventArgs">The key and modifier state reported by Avalonia.</param>
    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        string keyName = eventArgs.Key.ToString();
        int modifiers = ToLegacyModifiers(eventArgs.KeyModifiers);
        if (modifiers == 0 && keyName is "Delete" or "Back" or "Escape")
        {
            SetCurrentValue(HotkeyProperty, null);
            return;
        }

        if (IsModifierOnly(keyName) || !TryGetLegacyKey(keyName, out int key))
        {
            return;
        }

        SetCurrentValue(HotkeyProperty, new HotkeySettings(key, modifiers));
    }

    internal static bool TryGetLegacyKey(string keyName, out int key)
    {
        if (keyName.Length == 1 && keyName[0] is >= 'A' and <= 'Z')
        {
            key = 44 + keyName[0] - 'A';
            return true;
        }

        if (keyName.Length == 2 && keyName[0] == 'D' && char.IsAsciiDigit(keyName[1]))
        {
            key = 34 + keyName[1] - '0';
            return true;
        }

        if (keyName.StartsWith("NumPad", StringComparison.Ordinal) &&
            keyName.Length == 7 && char.IsAsciiDigit(keyName[6]))
        {
            key = 74 + keyName[6] - '0';
            return true;
        }

        if (keyName.Length is 2 or 3 && keyName[0] == 'F' &&
            int.TryParse(keyName.AsSpan(1), out int function) &&
            function is >= 1 and <= 24)
        {
            key = 89 + function;
            return true;
        }

        key = keyName switch
        {
            "Back" => 2,
            "Tab" => 3,
            "Enter" or "Return" => 6,
            "Escape" => 13,
            "Space" => 18,
            "PageUp" => 19,
            "PageDown" => 20,
            "End" => 21,
            "Home" => 22,
            "Left" => 23,
            "Up" => 24,
            "Right" => 25,
            "Down" => 26,
            "Insert" => 31,
            "Delete" => 32,
            "Multiply" => 84,
            "Add" => 85,
            "Separator" => 86,
            "Subtract" => 87,
            "Decimal" => 88,
            "Divide" => 89,
            "NumLock" => 114,
            "Scroll" or "ScrollLock" => 115,
            _ => 0
        };
        return key != 0;
    }

    internal static string Format(HotkeySettings? hotkey)
    {
        if (hotkey is null || hotkey.Key == 0)
        {
            return "< not set >";
        }

        List<string> parts = [];
        if ((hotkey.Modifiers & 2) != 0)
        {
            parts.Add("Ctrl");
        }
        if ((hotkey.Modifiers & 4) != 0)
        {
            parts.Add("Shift");
        }
        if ((hotkey.Modifiers & 1) != 0)
        {
            parts.Add("Alt");
        }
        if ((hotkey.Modifiers & 8) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(hotkey.Key));
        return string.Join(" + ", parts);
    }

    private static int ToLegacyModifiers(KeyModifiers modifiers)
    {
        int result = 0;
        if ((modifiers & KeyModifiers.Alt) != 0)
        {
            result |= 1;
        }
        if ((modifiers & KeyModifiers.Control) != 0)
        {
            result |= 2;
        }
        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            result |= 4;
        }
        if ((modifiers & KeyModifiers.Meta) != 0)
        {
            result |= 8;
        }
        return result;
    }

    private static bool IsModifierOnly(string keyName) =>
        keyName is "LeftCtrl" or "RightCtrl" or
            "LeftAlt" or "RightAlt" or
            "LeftShift" or "RightShift" or
            "LWin" or "RWin" or "LeftMeta" or "RightMeta" or
            "Clear" or "OemClear" or "Apps";

    private static string FormatKey(int key)
    {
        if (key is >= 34 and <= 43)
        {
            return $"D{key - 34}";
        }
        if (key is >= 44 and <= 69)
        {
            return ((char)('A' + key - 44)).ToString();
        }
        if (key is >= 74 and <= 83)
        {
            return $"NumPad{key - 74}";
        }
        if (key is >= 90 and <= 113)
        {
            return $"F{key - 89}";
        }

        return key switch
        {
            2 => "Back",
            3 => "Tab",
            6 => "Enter",
            13 => "Escape",
            18 => "Space",
            19 => "PageUp",
            20 => "PageDown",
            21 => "End",
            22 => "Home",
            23 => "Left",
            24 => "Up",
            25 => "Right",
            26 => "Down",
            31 => "Insert",
            32 => "Delete",
            84 => "Multiply",
            85 => "Add",
            86 => "Separator",
            87 => "Subtract",
            88 => "Decimal",
            89 => "Divide",
            114 => "NumLock",
            115 => "ScrollLock",
            _ => $"Key {key}"
        };
    }

    private void UpdateText() => Text = Format(Hotkey);
}
