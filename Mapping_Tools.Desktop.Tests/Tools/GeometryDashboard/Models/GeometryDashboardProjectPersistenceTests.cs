using System.Text.Json;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Models;

[TestClass]
public sealed class GeometryDashboardProjectPersistenceTests
{
    [TestMethod]
    public void Serialize_ProjectKeepRunning_WritesProjectLevelProperty()
    {
        // Arrange
        GeometryDashboardProject project = new() { KeepRunning = true };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);
        using JsonDocument document = JsonDocument.Parse(json);

        // Assert
        document.RootElement.GetProperty("KeepRunning").GetBoolean().Should().BeTrue();
        document.RootElement
            .GetProperty("CurrentPreferences")
            .TryGetProperty("KeepRunning", out _)
            .Should()
            .BeFalse();
    }

    [TestMethod]
    public void Deserialize_LegacyNestedKeepRunning_MigratesToProjectLevelProperty()
    {
        // Arrange
        const string json = """
                            {
                              "CurrentPreferences": {
                                "KeepRunning": true
                              }
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        GeometryDashboardProject project = serializer.Deserialize<GeometryDashboardProject>(json);

        // Assert
        project.KeepRunning.Should().BeTrue();
    }
}
