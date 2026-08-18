using FluentAssertions;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.GeometryDashboard;

[TestClass]
public sealed class GeometryDashboardContractsTests
{
    [TestMethod]
    public void GeometryDashboardProjectDefinition_CreateProject_UsesLegacyDefaultsAndPersistenceMetadata()
    {
        // Arrange
        var definition = GeometryDashboardProjectDefinition.Definition;

        // Act
        SnappingToolsProject project = definition.CreateProject();

        // Assert
        definition.AutoSaveFileName.Should().Be("geometrydashboardproject.json");
        definition.ProjectFolderName.Should().Be("Geometry Dashboard Projects");
        definition.SuggestedFileName.Should().Be("geometry-dashboard-project.json");
        project.CurrentPreferences.InceptionLevel.Should().Be(5);
    }
}
