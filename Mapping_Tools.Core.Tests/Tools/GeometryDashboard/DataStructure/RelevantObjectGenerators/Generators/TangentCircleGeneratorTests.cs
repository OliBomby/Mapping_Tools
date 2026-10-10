using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class TangentCircleGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithRepeatedPoints_ReturnsNoCircles()
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();
        RelevantPoint point = new(new Vector2(20, 0));

        // Act
        RelevantCircle[] circles = generator.GetRelevantObjects(source, point, point);

        // Assert
        circles.Should().BeEmpty();
    }

    [TestMethod]
    public void GetRelevantObjects_WithPointAtSourceCentre_ReturnsNoCircles()
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();

        // Act
        RelevantCircle[] circles = generator.GetRelevantObjects(source,
            new RelevantPoint(new Vector2(0, 0)), new RelevantPoint(new Vector2(20, 0)));

        // Assert
        circles.Should().BeEmpty();
    }

    [TestMethod]
    public void GetRelevantObjects_WithOnePointInsideAndOneOutside_ReturnsNoCircles()
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();

        // Act
        RelevantCircle[] circles = generator.GetRelevantObjects(source,
            new RelevantPoint(new Vector2(2, 0)), new RelevantPoint(new Vector2(20, 0)));

        // Assert
        circles.Should().BeEmpty();
    }

    [TestMethod]
    public void GetRelevantObjects_WithPointOnSourceCircle_ReturnsKnownTangentCircle()
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();

        // Act
        RelevantCircle[] circles = generator.GetRelevantObjects(source,
            new RelevantPoint(new Vector2(10, 0)), new RelevantPoint(new Vector2(20, 0)));

        // Assert
        RelevantCircle circle = circles.Should().ContainSingle().Subject;
        circle.Child.Centre.X.Should().BeApproximately(15, 0.000001);
        circle.Child.Centre.Y.Should().BeApproximately(0, 0.000001);
        circle.Child.Radius.Should().BeApproximately(5, 0.000001);
    }

    [DataTestMethod]
    [DataRow(2d, 0d, 0d, 2d)]
    [DataRow(20d, 0d, 20d, 10d)]
    public void GetRelevantObjects_WithBothPointsOnSameSide_ProducesCirclesThroughBothPointsAndTangentToSource(
        double x1, double y1, double x2, double y2)
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();
        Vector2 first = new(x1, y1);
        Vector2 second = new(x2, y2);

        // Act
        RelevantCircle[] circles = generator.GetRelevantObjects(source,
            new RelevantPoint(first), new RelevantPoint(second));

        // Assert
        circles.Should().NotBeEmpty();
        foreach (RelevantCircle result in circles)
        {
            Vector2 centre = result.Child.Centre;
            double radius = result.Child.Radius;
            Vector2.Distance(centre, first).Should().BeApproximately(radius, 0.000001);
            Vector2.Distance(centre, second).Should().BeApproximately(radius, 0.000001);
            double centreDistance = Vector2.Distance(centre, source.Child.Centre);
            bool externallyTangent = Math.Abs(centreDistance - (source.Child.Radius + radius)) < 0.000001;
            bool internallyTangent = Math.Abs(centreDistance - Math.Abs(source.Child.Radius - radius)) < 0.000001;
            (externallyTangent || internallyTangent).Should().BeTrue();
        }
    }

    [TestMethod]
    public void GetRelevantObjects_WithPointsSwapped_ProducesSameTangentSolutions()
    {
        // Arrange
        TangentCircleGenerator generator = new();
        RelevantCircle source = SourceCircle();
        RelevantPoint near = new(new Vector2(20, 0));
        RelevantPoint far = new(new Vector2(20, 10));

        // Act
        RelevantCircle[] forward = generator.GetRelevantObjects(source, near, far);
        RelevantCircle[] reverse = generator.GetRelevantObjects(source, far, near);

        // Assert
        forward.Should().NotBeEmpty();
        reverse.Should().HaveCount(forward.Length);
        foreach (RelevantCircle circle in forward)
            reverse.Should().Contain(other =>
                Vector2.Distance(other.Child.Centre, circle.Child.Centre) < 0.000001
                && Math.Abs(other.Child.Radius - circle.Child.Radius) < 0.000001);
    }

    private static RelevantCircle SourceCircle()
    {
        return new RelevantCircle(new Circle(new Vector2(0, 0), 10));
    }
}
