using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tmds.DBus.Protocol;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class XdgGlobalShortcutPortalTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("unix:path=/tmp/mapping-tools-missing-session-bus")]
    [DataRow("invalid-address")]
    public async Task IsAvailableAsync_WithoutReachableSessionBus_ReturnsFalse(string? busAddress)
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(1);

        // Act
        bool available = await XdgGlobalShortcutPortal.IsAvailableAsync(busAddress, timeout);

        // Assert
        available.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow("quick-run", "QuickRun")]
    [DataRow("quick-undo", "QuickUndo")]
    [DataRow("better-save", "BetterSave")]
    public void GetDescription_WithKnownAction_UsesActionNameWithoutPrefixOrSpaces(string id, string expected)
    {
        // Arrange
        // Act
        string description = XdgGlobalShortcutPortal.GetDescription(id);

        // Assert
        description.Should().Be(expected);
    }

    [TestMethod]
    public void ReadShortcuts_PortalResponseWithMultipleAndUnassignedTriggers_ConvertsDescriptionsToHotkeySettings()
    {
        // Arrange
        Dict<string, VariantValue> assigned = new() { ["description"] = "quick run", ["trigger_description"] = "Meta+F8, Ctrl+K" };
        Dict<string, VariantValue> unassigned = new() { ["description"] = "quick undo" };
        Array<Struct<string, Dict<string, VariantValue>>> shortcuts = new()
        {
            new("quick-run:44:2", assigned),
            new("quick-undo:69:2", unassigned),
        };
        Dictionary<string, VariantValue> results = new() { ["shortcuts"] = shortcuts.AsVariantValue() };

        // Act
        var descriptions = XdgGlobalShortcutPortal.ReadShortcuts(results);

        // Assert
        descriptions.Should().BeEquivalentTo(new Dictionary<string, HotkeySettings>
        {
            ["quick-run:44:2"] = new(97, 8),
            ["quick-undo:69:2"] = new(0, 0),
        });
    }
}
