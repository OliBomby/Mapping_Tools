using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SameTransformGenerator3Tests
{
    [TestMethod]
    public void GetRelevantObjects_WithQuarterTurn_ContinuesTurningInSameDirection()
    {
        // Arrange
        SameTransformGenerator3 generator = new();

        // Act
        RelevantPoint? next = generator.GetRelevantObjects(
            Point(0, 0), Point(10, 0), Point(10, 10));

        // Assert
        next.Should().NotBeNull();
        next!.Child.X.Should().BeApproximately(0, 0.000001);
        next.Child.Y.Should().BeApproximately(10, 0.000001);
    }

    [TestMethod]
    public void GetRelevantObjects_WithRepeatedFirstPair_ReturnsNoProjection()
    {
        // Arrange
        SameTransformGenerator3 generator = new();

        // Act
        RelevantPoint? next = generator.GetRelevantObjects(
            Point(0, 0), Point(0, 0), Point(10, 0));

        // Assert
        next.Should().BeNull();
    }

    private static RelevantPoint Point(double x, double y) => new(new Vector2(x, y));
}
