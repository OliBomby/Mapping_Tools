using Avalonia;
using Avalonia.Controls;
using Mapping_Tools.Desktop.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Controls;

/// <summary>Edits the legacy-compatible Geometry Dashboard key/modifier pair.</summary>
public sealed partial class GeometryHotkeyEditor : UserControl
{
    /// <summary>Defines the nullable Core hotkey property edited by the control.</summary>
    public static readonly StyledProperty<Hotkey?> HotkeyProperty =
        AvaloniaProperty.Register<GeometryHotkeyEditor, Hotkey?>(
            nameof(Hotkey), defaultBindingMode: BindingMode.TwoWay);

    static GeometryHotkeyEditor()
    {
        HotkeyProperty.Changed.AddClassHandler<GeometryHotkeyEditor>((control, _) => control.RefreshText());
    }

    /// <summary>Creates the hotkey editor.</summary>
    public GeometryHotkeyEditor()
    {
        InitializeComponent();
        RefreshText();
    }

    /// <summary>Gets or sets the nullable hotkey value.</summary>
    public Hotkey? Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    private void EditorKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        if (eventArgs.KeyModifiers == KeyModifiers.None && eventArgs.Key is Key.Delete or Key.Back or Key.Escape)
        {
            Hotkey = null;
            return;
        }

        if (eventArgs.Key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        if (eventArgs.Key is Key.Clear or Key.OemClear or Key.Apps || !HotkeyEditor.TryGetLegacyKey(eventArgs.Key, out int legacyKey))
            return;

        int modifiers = ToLegacyModifiers(eventArgs.KeyModifiers);
        Hotkey = new Hotkey(legacyKey, modifiers);
    }

    private void RefreshText()
    {
        if (Editor is null) return;
        Editor.Text = HotkeyEditor.FormatLegacy(Hotkey?.Key ?? 0, Hotkey?.Modifiers ?? 0);
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
}
