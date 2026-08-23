using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.PatternGallery;

[TestClass]
public sealed class PatternGalleryProjectSerializerTests
{
    [TestMethod]
    public void Deserialize_WithLegacyPatternGalleryDocument_RestoresCollectionAndPattern()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.PatternGalleryVm, Mapping Tools",
                              "CollectionName": "Legacy collection",
                              "Patterns": [
                                {
                                  "$type": "Mapping_Tools.Classes.Tools.PatternGallery.OsuPattern, Mapping Tools",
                                  "Name": "Legacy pattern",
                                  "FileName": "legacy.osu",
                                  "ObjectCount": 3
                                }
                              ],
                              "FileHandler": {
                                "$type": "Mapping_Tools.Classes.Tools.PatternGallery.OsuPatternFileHandler, Mapping Tools",
                                "CollectionFolderName": "LegacyFolder"
                              }
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<PatternGalleryProject>(json);

        // Assert
        project.CollectionName.Should().Be("Legacy collection");
        project.Patterns.Should().ContainSingle();
        project.Patterns[0].Name.Should().Be("Legacy pattern");
        project.Patterns[0].FileName.Should().Be("legacy.osu");
        project.FileHandler.CollectionFolderName.Should().Be("LegacyFolder");
    }

    [TestMethod]
    public void Serialize_WithPatternGalleryProject_UsesLegacyTypeNames()
    {
        // Arrange
        PatternGalleryProject project = new();
        project.Patterns.Add(new PatternGalleryPattern { Name = "Pattern", FileName = "pattern.osu" });
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.PatternGalleryVm, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.PatternGallery.OsuPattern, Mapping Tools");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroPatternGalleryFixture_RestoresCollectionAndPatternFile()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Patterns",
            "legacy-collection",
            "project.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<PatternGalleryProject>(File.ReadAllText(fixture));

        // Assert
        project.CollectionName.Should().Be("My Pattern Collection");
        project.Patterns.Should().ContainSingle();
        project.Patterns[0].Name.Should().Be("Pattern 1");
        project.Patterns[0].FileName.Should().Be("2025-06-07 21-32-54_NEsChy2u__Pattern 1.osu");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroPatternGalleryProjectFixture_RestoresProjectHistory()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "patterngalleryproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<PatternGalleryProject>(File.ReadAllText(fixture));

        // Assert
        project.CollectionName.Should().Be("My Pattern Collection");
        project.Patterns.Should().NotBeEmpty();
        project.Patterns.Should().Contain(pattern => pattern.Name == "Pattern OVAESDLFKJA");
        project.FileHandler.CollectionFolderName.Should().Be("NlLGMd6W0GRmEaMyE1pq");
    }
}
