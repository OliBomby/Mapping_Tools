using Avalonia.Input;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class HotkeyEditorTests
{
    [TestMethod]
    public void TryGetLegacyKey_WithSupportedAvaloniaNames_ReturnsWpfKeyValues()
    {
        // Arrange
        string[] names =
            ["A", "D7", "NumPad3", "F12", "Delete", "BrowserBack", "MediaNextTrack", "OemPlus"];

        // Act
        int[] values = names.Select(name =>
        {
            HotkeyEditor.TryGetLegacyKey(name, out int value).Should().BeTrue();
            return value;
        }).ToArray();

        // Assert
        values.Should().Equal(44, 41, 77, 101, 32, 122, 132, 141);
    }

    [TestMethod]
    public void Format_WithLegacyModifiers_UsesStableReadableOrder()
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
    public void ApplyKey_WithUnmodifiedEscape_ClearsHotkeyAndUpdatesDisplay()
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
