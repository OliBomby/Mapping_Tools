using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Captures one Avalonia key gesture while persisting the legacy WPF numeric key format.
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
        HotkeyProperty.Changed.AddClassHandler<HotkeyEditor>(static (editor, _) => editor.UpdateText());
    }

    /// <summary>Creates a read-only field whose keyboard input edits only the hotkey value.</summary>
    public HotkeyEditor()
    {
        IsReadOnly = true;
        IsUndoEnabled = false;
        InnerRightContent = null;
        CaretBrush = Brushes.Transparent;
        ContextFlyout = null;
        TextAlignment = TextAlignment.Center;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
        UpdateText();
    }

    /// <summary>
    ///     Gets or sets the persisted key and legacy Alt, Control, Shift, and Windows modifier bits.
    /// </summary>
    public HotkeySettings? Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    /// <summary>
    ///     Captures supported keys, ignores modifier-only presses, and clears on unmodified Delete,
    ///     Backspace, or Escape.
    /// </summary>
    /// <param name="eventArgs">The key and modifier state reported by Avalonia.</param>
    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        ApplyKey(eventArgs.Key, eventArgs.KeyModifiers);
    }

    internal void ApplyKey(string keyName, KeyModifiers keyModifiers)
    {
        if (Enum.TryParse(keyName, out Key key)) ApplyKey(key, keyModifiers);
    }

    internal void ApplyKey(Key key, KeyModifiers keyModifiers)
    {
        int modifiers = ToLegacyModifiers(keyModifiers);
        if (modifiers == 0 && key is Key.Delete or Key.Back or Key.Escape)
        {
            SetCurrentValue(HotkeyProperty, null);
            UpdateText();
            return;
        }

        if (IsUnsupportedKey(key) || !TryGetLegacyKey(key, out int legacyKey)) return;

        SetCurrentValue(HotkeyProperty, new HotkeySettings(legacyKey, modifiers));
    }

    internal static bool TryGetLegacyKey(string keyName, out int key)
    {
        if (Enum.TryParse(keyName, out Key avaloniaKey)) return TryGetLegacyKey(avaloniaKey, out key);

        key = 0;
        return false;
    }

    internal static bool TryGetLegacyKey(Key avaloniaKey, out int key)
    {
        key = (int)avaloniaKey;
        return key is >= 1 and <= 171 && !IsUnsupportedKey(avaloniaKey);
    }

    internal static string Format(HotkeySettings? hotkey)
    {
        if (hotkey is null || hotkey.Key == 0) return "< not set >";

        List<string> parts = [];
        if ((hotkey.Modifiers & 2) != 0) parts.Add("Ctrl");
        if ((hotkey.Modifiers & 4) != 0) parts.Add("Shift");
        if ((hotkey.Modifiers & 1) != 0) parts.Add("Alt");
        if ((hotkey.Modifiers & 8) != 0) parts.Add("Win");

        parts.Add(FormatKey(hotkey.Key));
        return string.Join(" + ", parts);
    }

    private static int ToLegacyModifiers(KeyModifiers modifiers)
    {
        int result = 0;
        if ((modifiers & KeyModifiers.Alt) != 0) result |= 1;
        if ((modifiers & KeyModifiers.Control) != 0) result |= 2;
        if ((modifiers & KeyModifiers.Shift) != 0) result |= 4;
        if ((modifiers & KeyModifiers.Meta) != 0) result |= 8;
        return result;
    }

    private static bool IsUnsupportedKey(Key key)
    {
        return key is
            Key.None or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or
            Key.LWin or Key.RWin or
            Key.Clear or Key.OemClear or Key.Apps or
            Key.ImeProcessed or Key.System or Key.DeadCharProcessed;
    }

    private static string FormatKey(int key)
    {
        return key is >= 1 and <= 171 && Enum.IsDefined((Key)key)
            ? ((Key)key).ToString()
            : $"Key {key}";
    }

    private void UpdateText()
    {
        Text = Format(Hotkey);
    }
}
