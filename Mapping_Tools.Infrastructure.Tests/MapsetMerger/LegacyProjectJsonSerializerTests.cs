using FluentAssertions;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.MapsetMerger;

[TestClass]
public sealed class LegacyProjectJsonSerializerTests
{
    [TestMethod]
    public void Deserialize_WithLegacyMapsetMergerDocument_RestoresProjectAndItems()
    {
        // Arrange
        const string json = """
            {
              "$type": "Mapping_Tools.Viewmodels.MapsetMergerVm, Mapping Tools",
              "ExportPath": "C:\\Export",
              "MoveSbToBeatmap": true,
              "Mapsets": [
                {
                  "$type": "Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem, Mapping Tools",
                  "Name": "Pack",
                  "Path": "C:\\Pack"
                }
              ]
            }
            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        MapsetMergerProject project = serializer.Deserialize<MapsetMergerProject>(json);

        // Assert
        project.ExportPath.Should().Be("C:\\Export");
        project.MoveSbToBeatmap.Should().BeTrue();
        project.Mapsets.Should().ContainSingle();
        project.Mapsets[0].Name.Should().Be("Pack");
        project.Mapsets[0].Path.Should().Be("C:\\Pack");
    }

    [TestMethod]
    public void Serialize_WithMapsetMergerProject_UsesLegacyTypeNames()
    {
        // Arrange
        MapsetMergerProject project = new()
        {
            ExportPath = "C:\\Export",
            Mapsets = [new() { Name = "Pack", Path = "C:\\Pack" }]
        };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.MapsetMergerVm, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem, Mapping Tools");
    }
}
