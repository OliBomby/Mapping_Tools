using System.Globalization;
using Mapping_Tools.Core.Tools.TimingCopier;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class TimingCopierResnapModeConverterTests
{
    [TestMethod]
    public void Convert_WithEachResnapMode_ReturnsItsDisplayName()
    {
        // Arrange
        TimingCopierResnapModeConverter converter = new();

        // Act
        object[] displayNames = Enum.GetValues<TimingCopierResnapMode>()
            .Select(mode => converter.Convert(
                mode,
                typeof(string),
                null,
                CultureInfo.InvariantCulture))
            .ToArray();

        // Assert
        displayNames.Should().Equal(
            "Number of beats between objects stays the same",
            "Just resnap",
            "Don't move objects");
    }
}
