using Avalonia.Data;
using Avalonia.Input;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class HotkeyEditorTests
{
    [TestMethod]
    public void Hotkey_DesktopAssignmentChanges_DisplaysCurrentGesture()
    {
        // Arrange
        HotkeyEditor editor = new() { Hotkey = new HotkeySettings(44, 2) };

        // Act
        editor.Hotkey = new HotkeySettings(97, 8);

        // Assert
        editor.Text.Should().Be("Win + F8");
        editor.Hotkey.Should().Be(new HotkeySettings(97, 8));
    }

    [TestMethod]
    public void Hotkey_UnassignedDesktopShortcut_ShowsNotSetWithoutRetainingRequestedGesture()
    {
        // Arrange
        HotkeyEditor editor = new() { Hotkey = new HotkeySettings(44, 2) };

        // Act
        editor.Hotkey = new HotkeySettings(0, 0);

        // Assert
        editor.Text.Should().Be("< not set >");
        editor.Hotkey!.Key.Should().Be(0);
    }

    [TestMethod]
    public void ApplyKey_WithDesktopAssignment_UpdatesGestureImmediately()
    {
        // Arrange
        HotkeyEditor editor = new() { Hotkey = new HotkeySettings(97, 8) };

        // Act
        editor.ApplyKey("B", KeyModifiers.Control);

        // Assert
        editor.Text.Should().Be("Ctrl + B");
        editor.Hotkey.Should().Be(new HotkeySettings(45, 2));
    }

    [TestMethod]
    public void Hotkey_LostFocusBindingWithSeveralCapturedGestures_CommitsOnlyTheLastGestureOnce()
    {
        // Arrange
        HotkeyEditor source = new() { Hotkey = new HotkeySettings(44, 2) };
        HotkeyEditor editor = new();
        editor.Bind(HotkeyEditor.HotkeyProperty, new Binding(nameof(HotkeyEditor.Hotkey))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
        });
        int changes = 0;
        source.PropertyChanged += (_, args) =>
        {
            if (args.Property == HotkeyEditor.HotkeyProperty) changes++;
        };

        // Act
        editor.ApplyKey("B", KeyModifiers.Control);
        editor.ApplyKey("C", KeyModifiers.Alt);
        int changesBeforeFocusLoss = changes;
        editor.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent) { Source = editor });
        editor.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent) { Source = editor });

        // Assert
        changesBeforeFocusLoss.Should().Be(0);
        changes.Should().Be(1);
        source.Hotkey.Should().Be(new HotkeySettings(46, 1));
        editor.Hotkey.Should().Be(new HotkeySettings(46, 1));
        editor.Text.Should().Be("Alt + C");
    }

    [TestMethod]
    public void Hotkey_LostFocusBindingWithExternalAssignment_DoesNotOverwriteDesktopAssignment()
    {
        // Arrange
        HotkeyEditor source = new() { Hotkey = new HotkeySettings(44, 2) };
        HotkeyEditor editor = new();
        editor.Bind(HotkeyEditor.HotkeyProperty, new Binding(nameof(HotkeyEditor.Hotkey))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
        });
        editor.ApplyKey("B", KeyModifiers.Control);

        // Act
        source.Hotkey = new HotkeySettings(97, 8);
        editor.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent) { Source = editor });

        // Assert
        editor.Hotkey.Should().Be(new HotkeySettings(97, 8));
        editor.Text.Should().Be("Win + F8");
        source.Hotkey.Should().Be(new HotkeySettings(97, 8));
    }

    [TestMethod]
    public void TryGetKey_WithSupportedAvaloniaNames_ReturnsAvaloniaKeyValues()
    {
        // Arrange
        string[] names =
            ["A", "D7", "NumPad3", "F12", "Delete", "BrowserBack", "MediaNextTrack", "OemPlus"];

        // Act
        int[] values = names.Select(name =>
        {
            if (!HotkeyEditor.TryGetKey(name, out int value))
                throw new InvalidOperationException($"The key name '{name}' was not supported.");

            return value;
        }).ToArray();

        // Assert
        values.Should().Equal(44, 41, 77, 101, 32, 122, 132, 141);
    }

    [TestMethod]
    public void Format_WithAllModifiers_UsesStableReadableOrder()
    {
        // Arrange
        HotkeySettings hotkey = new(56, 15);

        // Act
        string display = HotkeyEditor.Format(hotkey);

        // Assert
        display.Should().Be("Ctrl + Shift + Alt + Win + M");
    }

    [TestMethod]
    public void Format_WithoutBinding_ShowsNotSetPlaceholder()
    {
        // Arrange
        HotkeySettings? hotkey = null;

        // Act
        string display = HotkeyEditor.Format(hotkey);

        // Assert
        display.Should().Be("< not set >");
    }

    [TestMethod]
    public void ApplyKey_WithEscape_ClearsHotkeyAndUpdatesDisplay()
    {
        // Arrange
        HotkeyEditor editor = new()
        {
            Hotkey = new HotkeySettings(56, 2),
        };

        // Act
        editor.ApplyKey("Escape", KeyModifiers.None);

        // Assert
        editor.Hotkey.Should().BeNull();
        editor.Text.Should().Be("< not set >");
    }
}
