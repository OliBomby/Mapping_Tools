using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class ComboColourProjectPersistenceTests
{
    [TestMethod]
    public void Deserialize_LegacyComboColourProject_PreservesSequencesAndModes()
    {
        // Arrange
        string json = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "combocolourproject.json"));

        LegacyProjectJsonSerializer serializer = new();
        // Act
        var project = serializer.Deserialize<ComboColourEngineOptions>(json);

        // Assert
        project.ComboColours.Should().HaveCount(4);
        project.ColourPoints.Should().NotBeEmpty();
        project.ColourPoints[0].ColourSequence.Select(colour => colour.Name)
            .Should().Equal("Combo2", "Combo3", "Combo4");
        project.ColourPoints.Should().Contain(point => point.Mode == ColourPointMode.Burst);
    }

    [TestMethod]
    public void Serialize_ComboColourProject_UsesLegacyTypeNames()
    {
        // Arrange
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.AddColourPoint(20, [project.ComboColours[0]], ColourPointMode.Burst);

        // Act
        string json = new LegacyProjectJsonSerializer().Serialize(project);

        // Assert
        json.Should().Contain("Mapping_Tools.Classes.Tools.ComboColourStudio.ComboColourEngineOptions");
        json.Should().Contain("Mapping_Tools.Classes.Tools.ComboColourStudio.ColourPoint");
    }
}
