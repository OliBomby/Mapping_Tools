using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Infrastructure.Editor.Mtipc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor.Mtipc;

[TestClass]
public sealed class MtipcLiveBeatmapReaderTests
{
    [TestMethod]
    [DataRow(1, 1500, 500.0, 156.8, 1464.0)]
    [DataRow(2, 2000, 500.0, 71.2, 1964.0)]
    [DataRow(1, 1060, 60.0, 114.0, 1030.0)]
    public void ConvertHitObject_WithSliderEndTime_PreservesDurationAndTrueSliderEnd(
        int spans, int endTime, double spanDuration, double expectedX, double expectedTime)
    {
        // Arrange
        MtipcHitObjectData source = new(100, 1000, endTime, 2, 0, spans,
            new Vector2(64, 96), new Vector2(164, 96), null, 100, 0, 0, 0, true, 2,
            [new Vector2(64, 96), new Vector2(164, 96)], [], [], []);

        // Act
        var converted = MtipcLiveBeatmapReader.ConvertHitObject(source);
        var point = new TrueSliderEndGenerator().GetRelevantObjects(new RelevantHitObject(converted));

        // Assert
        converted.Repeat.Should().Be(spans);
        converted.TemporalLength.Should().Be(spanDuration);
        converted.EndTime.Should().Be(endTime);
        point.Should().NotBeNull();
        point.Child.X.Should().BeApproximately(expectedX, 0.000001);
        point.Child.Y.Should().Be(96);
        point.CustomTime.Should().Be(expectedTime);
    }
}
