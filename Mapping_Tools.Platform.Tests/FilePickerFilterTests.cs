using Mapping_Tools.Application.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

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
        filter.Patterns.ToArray().Should().Equal(new[] { "*.osu", "*.osb" });
        filter.MimeTypes.ToArray().Should().Equal(new[] { "application/x-osu-beatmap" });
        filter.AppleUniformTypeIdentifiers.ToArray().Should().Equal(new[] { "public.data" });
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
}
