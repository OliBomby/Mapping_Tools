using Mapping_Tools.Application.Platform.FilePicker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Platform;

[TestClass]
public sealed class FilePickerFilterTests
{
    [TestMethod]
    public void Constructor_WithDirtyDuplicateValues_CleansAndDeduplicates()
    {
        // Arrange
        // Act
        FilePickerFilter filter = new(
            "Beatmaps",
            ["*.osu", "", "*.OSU", "  *.osb  "],
            ["application/x-osu-beatmap", " "],
            ["public.data"]);

        // Assert
        filter.Patterns.ToArray().Should().Equal("*.osu", "*.osb");
        filter.MimeTypes.ToArray().Should().Equal("application/x-osu-beatmap");
        filter.AppleUniformTypeIdentifiers.ToArray().Should().Equal("public.data");
    }

    [TestMethod]
    public void Constructor_WithEmptyPatterns_ThrowsArgumentException()
    {
        // Arrange
        // Act
        Action act1 = () => new FilePickerFilter("Beatmaps", ["", " "]);

        // Assert
        act1.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void CommonFilters_PreserveExistingPickerContracts()
    {
        // Arrange
        // Act
        var beatmaps = CommonFilePickerFilters.Beatmaps;
        var beatmapsAndStoryboards = CommonFilePickerFilters.BeatmapsAndStoryboards;
        var beatmapBackups = CommonFilePickerFilters.BeatmapBackups;
        var projects = CommonFilePickerFilters.MappingToolsProjects;
        var configuration = CommonFilePickerFilters.OsuConfiguration;

        // Assert
        beatmaps.Name.Should().Be("osu! beatmap");
        beatmaps.Patterns.Should().Equal("*.osu");
        beatmaps.MimeTypes.Should().Equal("application/x-osu-beatmap");
        beatmapsAndStoryboards.Name.Should().Be("osu! beatmaps and storyboards");
        beatmapsAndStoryboards.Patterns.Should().Equal("*.osu", "*.osb");
        beatmapsAndStoryboards.MimeTypes.Should().Equal("application/x-osu-beatmap", "text/plain");
        beatmapBackups.Name.Should().Be("osu! beatmap backups");
        beatmapBackups.Patterns.Should().Equal("*.osu", "*.osb");
        projects.Name.Should().Be("Mapping Tools project");
        projects.Patterns.Should().Equal("*.json");
        configuration.Name.Should().Be("osu! user configuration");
        configuration.Patterns.Should().Equal("osu!.*.cfg");
    }
}
