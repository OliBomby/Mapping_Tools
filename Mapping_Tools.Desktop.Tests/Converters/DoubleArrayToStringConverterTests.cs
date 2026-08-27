using System.Globalization;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class DoubleArrayToStringConverterTests
{
    [TestMethod]
    public void DoubleArrayToStringConverter_WithInvariantValues_RoundTripsText()
    {
        // Arrange
        DoubleArrayToStringConverter converter = new();

        // Act
        object text = converter.Convert(
            new[] { 1.25, 2.5 },
            typeof(string),
            null,
            CultureInfo.GetCultureInfo("nl-NL"));
        object values = converter.ConvertBack(
            text,
            typeof(double[]),
            null,
            CultureInfo.GetCultureInfo("nl-NL"));

        // Assert
        text.Should().Be("1.25, 2.5");
        values.Should().BeEquivalentTo(new[] { 1.25, 2.5 });
    }
}
