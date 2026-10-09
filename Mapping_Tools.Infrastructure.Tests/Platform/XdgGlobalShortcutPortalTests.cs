using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tmds.DBus.Protocol;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class XdgGlobalShortcutPortalTests
{
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
