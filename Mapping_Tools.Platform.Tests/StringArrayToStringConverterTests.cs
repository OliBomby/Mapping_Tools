using System.Globalization;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class StringArrayToStringConverterTests
{
    [TestMethod]
    public void Convert_WithOrderedPaths_JoinsUsingLegacySeparator()
    {
        // Arrange
        StringArrayToStringConverter converter = new();

        // Act
        object result = converter.Convert(
            new[] { "first.osu", "second.osu" },
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("first.osu|second.osu");
    }

    [TestMethod]
    public void ConvertBack_WithWhitespaceAndEmptySegments_ReturnsCleanPathArray()
    {
        // Arrange
        StringArrayToStringConverter converter = new();

        // Act
        object result = converter.ConvertBack(
            " first.osu | | second.osu ",
            typeof(string[]),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<string[]>()
            .Which.Should().Equal("first.osu", "second.osu");
    }
}
