using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class XdgShortcutTriggerTests
{
    [DataTestMethod]
    [DataRow("Meta+F8, Ctrl+K", 97, 8)]
    [DataRow("Ctrl+Shift+Alt+Meta+Z", 69, 15)]
    [DataRow("CTRL+Return", 6, 2)]
    [DataRow("Alt+PgDown", 20, 1)]
    [DataRow("Ctrl+Num+3", 77, 2)]
    [DataRow("Ctrl+Num++", 85, 2)]
    [DataRow("Ctrl++", 141, 2)]
    [DataRow("Ctrl+,, Ctrl+K", 142, 2)]
    [DataRow("Ctrl+Space", 18, 2)]
    [DataRow("Strg+Umschalt+A", 44, 6)]
    [DataRow("Press <ctrl><alt><shift><super>a or <ctrl>b", 44, 15)]
    [DataRow("Druk op <super>F8 of <ctrl>k", 97, 8)]
    [DataRow("Press a or b", 44, 0)]
    [DataRow("XF86AudioMute", 129, 0)]
    [DataRow("", 0, 0)]
    [DataRow("  ", 0, 0)]
    public void ParseDescription_DesktopAssignment_ConvertsFirstGestureToSettings(string description, int key, int modifiers)
    {
        // Arrange
        // Act
        HotkeySettings result = XdgShortcutTrigger.ParseDescription(description);

        // Assert
        result.Should().Be(new HotkeySettings(key, modifiers));
    }

    [DataTestMethod]
    [DataRow("Ctrl+UnknownKey")]
    [DataRow("UnknownModifier+A")]
    [DataRow("Press <ctrlA")]
    public void ParseDescription_UnrecognizedAssignment_ReportsConversionFailure(string description)
    {
        // Arrange
        // Act
        Action act = () => XdgShortcutTrigger.ParseDescription(description);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [DataTestMethod]
    [DataRow(44, 2, "CTRL+a")]
    [DataRow(69, 15, "CTRL+ALT+SHIFT+LOGO+z")]
    [DataRow(18, 3, "CTRL+ALT+space")]
    [DataRow(6, 2, "CTRL+Return")]
    [DataRow(43, 0, "9")]
    [DataRow(74, 0, "KP_0")]
    [DataRow(83, 0, "KP_9")]
    [DataRow(90, 0, "F1")]
    [DataRow(113, 4, "SHIFT+F24")]
    [DataRow(140, 2, "CTRL+semicolon")]
    [DataRow(150, 8, "LOGO+backslash")]
    [DataRow(129, 0, "XF86AudioMute")]
    public void Convert_WithPersistedAvaloniaKey_UsesXdgKeysymAndModifiers(int key, int modifiers, string expected)
    {
        // Arrange
        HotkeySettings hotkey = new(key, modifiers);

        // Act
        string? result = XdgShortcutTrigger.Convert(hotkey);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_WithUnmappedKey_LetsDesktopChooseTrigger()
    {
        // Arrange
        HotkeySettings hotkey = new(153, 2);

        // Act
        string? result = XdgShortcutTrigger.Convert(hotkey);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WithUnknownModifiers_RejectsInvalidSettings()
    {
        // Arrange
        HotkeySettings hotkey = new(44, 16);

        // Act
        Action act = () => XdgShortcutTrigger.Convert(hotkey);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
