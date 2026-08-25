using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools;

[TestClass]
public sealed class ToolDefinitionTests
{
    [TestMethod]
    public void MappingToolDefinitions_TimingHelper_ExposesCanonicalMetadata()
    {
        // Arrange

        // Act
        ToolDefinition definition = MappingToolDefinitions.TimingHelper;

        // Assert
        definition.Id.Should().Be("timing-helper");
        definition.DisplayName.Should().Be("Timing Helper");
        definition.Description.Should().Be(
            "Adjust BPM and add redlines so selected markers become snapped.");
        definition.SearchTerms.Should().Equal(
            "timing",
            "redlines",
            "BPM",
            "markers",
            "beat divisors");
        definition.QuickRunTargets.Should().Be(QuickRunTargets.Always);
    }

    [TestMethod]
    public void MappingToolDefinitions_MapsetMerger_DeclaresNoQuickRunCommand()
    {
        // Arrange

        // Act
        ToolDefinition definition = MappingToolDefinitions.MapsetMerger;

        // Assert
        definition.QuickRunTargets.Should().BeNull();
    }

    [TestMethod]
    public void QuickRunCommand_FromToolDefinition_UsesCanonicalIdentityAndTargets()
    {
        // Arrange
        ToolDefinition definition = MappingToolDefinitions.TimingHelper;

        // Act
        QuickRunCommand command = new(definition, _ => Task.CompletedTask);

        // Assert
        command.Id.Should().Be(definition.Id);
        command.DisplayName.Should().Be(definition.DisplayName);
        command.Targets.Should().Be(definition.QuickRunTargets);
    }
}
