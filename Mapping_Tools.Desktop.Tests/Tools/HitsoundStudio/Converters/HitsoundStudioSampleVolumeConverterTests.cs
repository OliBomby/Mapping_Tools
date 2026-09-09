using System.Globalization;
using Avalonia.Data;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.Converters;

[TestClass]
public sealed class HitsoundStudioSampleVolumeConverterTests
{
    [TestMethod]
    public void Convert_UnityVolume_ReturnsOneHundredPercent()
    {
        // Arrange
        HitsoundStudioSampleVolumeConverter converter = new();

        // Act
        object result = converter.Convert(1d, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("100");
    }

    [TestMethod]
    public void Convert_InvariantVolume_ReturnsNegativeOne()
    {
        // Arrange
        HitsoundStudioSampleVolumeConverter converter = new();

        // Act
        object result = converter.Convert(-0.01d, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("-1");
    }

    [TestMethod]
    public void ConvertBack_PercentageText_ReturnsLinearVolume()
    {
        // Arrange
        HitsoundStudioSampleVolumeConverter converter = new();

        // Act
        object result = converter.ConvertBack("25", typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(0.25d);
    }

    [TestMethod]
    public void ConvertBack_InvariantPercentageText_ReturnsInvariantVolume()
    {
        // Arrange
        HitsoundStudioSampleVolumeConverter converter = new();

        // Act
        object result = converter.ConvertBack("-1", typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(-0.01d);
    }

    [TestMethod]
    public void ConvertBack_InvalidText_ReturnsDataValidationError()
    {
        // Arrange
        HitsoundStudioSampleVolumeConverter converter = new();

        // Act
        object result = converter.ConvertBack("not a volume", typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<BindingNotification>();
        ((BindingNotification)result).ErrorType.Should().Be(BindingErrorType.DataValidationError);
    }
}
