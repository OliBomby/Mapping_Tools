using Mapping_Tools.Core.Progress;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Progress;

[TestClass]
public sealed class ProgressExtensionsTests
{
    [TestMethod]
    public void MapTo_ValuesWithinRange_ForwardsMappedValues()
    {
        // Arrange
        RecordingProgress destination = new();
        IProgress<double> mapped = destination.MapTo(0.5, 1);

        // Act
        mapped.Report(0);
        mapped.Report(0.25);
        mapped.Report(1);

        // Assert
        destination.Values.Should().Equal(0.5, 0.625, 1);
    }

    [TestMethod]
    public void MapTo_NestedRanges_ComposesMappings()
    {
        // Arrange
        RecordingProgress destination = new();
        IProgress<double> mapped = destination.MapTo(0.5, 1).MapTo(0.25, 0.75);

        // Act
        mapped.Report(0);
        mapped.Report(1);

        // Assert
        destination.Values.Should().Equal(0.625, 0.875);
    }

    [TestMethod]
    public void MapTo_ZeroBasedStepAndTotal_MapsToStepRange()
    {
        // Arrange
        RecordingProgress destination = new();
        IProgress<double> mapped = destination.MapTo(1, 2);

        // Act
        mapped.Report(0);
        mapped.Report(1);

        // Assert
        destination.Values.Should().Equal(0.5, 1);
    }

    [TestMethod]
    public void Report_CompletedStepAndTotal_ReportsNormalizedProgress()
    {
        // Arrange
        RecordingProgress destination = new();

        // Act
        destination.Report(1, 2);
        destination.Report(2, 2);

        // Assert
        destination.Values.Should().Equal(0.5, 1);
    }

    [TestMethod]
    public void MapTo_ReversedRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        RecordingProgress destination = new();

        // Act
        Action act = () => destination.MapTo(0.75, 0.25);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void MapTo_StepOutsideTotal_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        RecordingProgress destination = new();

        // Act
        Action act = () => destination.MapTo(2, 2);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Report_StepOutsideTotal_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        RecordingProgress destination = new();

        // Act
        Action act = () => destination.Report(3, 2);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Report_ValueOutsideNormalizedRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        RecordingProgress destination = new();
        IProgress<double> mapped = destination.MapTo(0, 1);

        // Act
        Action act = () => mapped.Report(1.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class RecordingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value)
        {
            Values.Add(value);
        }
    }
}
