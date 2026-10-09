using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class KdeGlobalShortcutsTests
{
    [DataTestMethod]
    [DataRow(56, 1, 0x0800004d)]
    [DataRow(45, 3, 0x0c000042)]
    [DataRow(97, 8, 0x11000037)]
    [DataRow(77, 2, 0x24000033)]
    [DataRow(132, 0, 0x01000083)]
    [DataRow(140, 4, 0x0200003b)]
    public void ConvertKey_WithSupportedGesture_UsesQtKeyAndModifierBits(int key, int modifiers, int expected)
    {
        // Arrange
        HotkeySettings hotkey = new(key, modifiers);

        // Act
        int result = KdeGlobalShortcuts.ConvertKey(hotkey);

        // Assert
        result.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("quick-run:56:1", true)]
    [DataRow("quick-undo:69:2", true)]
    [DataRow("better-save:62:2", true)]
    [DataRow("quick-run", false)]
    [DataRow("other:56:1", false)]
    [DataRow("quick-run:custom:1", false)]
    public void IsLegacyId_WithRegistrationId_RecognizesOnlyOldMappingToolsActions(string id, bool expected)
    {
        // Arrange
        // Act
        bool result = KdeGlobalShortcuts.IsLegacyId(id);

        // Assert
        result.Should().Be(expected);
    }
}
