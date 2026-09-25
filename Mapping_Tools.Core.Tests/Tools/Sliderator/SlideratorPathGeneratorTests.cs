using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.Sliderator;

[TestClass]
public sealed class SlideratorPathGeneratorTests
{
    [TestMethod]
    public void SetPath_WithNullOrEmptyPoints_RejectsPath()
    {
        // Arrange
        SlideratorPathGenerator generator = new();

        // Act
        var nullPath = () => generator.SetPath(null!);
        var emptyPath = () => generator.SetPath([]);

        // Assert
        nullPath.Should().Throw<ArgumentNullException>();
        emptyPath.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SetPath_WithOnlyRepeatedPoints_RejectsZeroLengthPath()
    {
        // Arrange
        SlideratorPathGenerator generator = new();

        // Act
        var act = () => generator.SetPath([new Vector2(2, 3), new Vector2(2, 3)]);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Zero length*");
    }

    [TestMethod]
    public void SliderateStream_WithRepeatedVertices_FollowsEachSegmentAtExactTickPositions()
    {
        // Arrange
        SlideratorPathGenerator generator = new()
        {
            MaxT = 4,
            PositionFunction = time => time * 5,
        };
        generator.SetPath([
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 0),
            new Vector2(10, 0), new Vector2(10, 10),
        ]);

        // Act
        List<Vector2> points = generator.SliderateStream(1);

        // Assert
        points.Should().Equal([
            new Vector2(0, 0), new Vector2(5, 0), new Vector2(10, 0),
            new Vector2(10, 5), new Vector2(10, 10),
        ]);
    }

    [TestMethod]
    public void SliderateStream_WithDistancesOutsidePath_ExtrapolatesFirstAndLastSegments()
    {
        // Arrange
        SlideratorPathGenerator generator = new()
        {
            MaxT = 2,
            PositionFunction = time => time * 15 - 5,
        };
        generator.SetPath([new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        List<Vector2> points = generator.SliderateStream(1);

        // Assert
        points.Should().Equal([new Vector2(-5, 0), new Vector2(10, 0), new Vector2(25, 0)]);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void SliderateStream_WithInvalidInterval_Throws(double interval)
    {
        // Arrange
        SlideratorPathGenerator generator = new();
        generator.SetPath([new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        var act = () => generator.SliderateStream(interval);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Sliderate_WithStraightPathAndConstantSpeed_GeneratesFiniteAnchorsAtEndpoints()
    {
        // Arrange
        SlideratorPathGenerator generator = new()
        {
            MaxT = 1,
            Velocity = 10,
            PositionFunction = time => time * 10,
        };
        generator.SetPath([new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        List<Vector2> anchors = generator.Sliderate();

        // Assert
        generator.MaxS.Should().Be(10);
        anchors.Should().NotBeEmpty();
        anchors[0].Should().Be(new Vector2(0, 0));
        anchors[^1].X.Should().BeGreaterThanOrEqualTo(9);
        anchors[^1].Y.Should().Be(0);
        anchors.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }

    [TestMethod]
    public void Sliderate_WithReversingPositionFunction_ReturnsTowardStartAfterTurnaround()
    {
        // Arrange
        SlideratorPathGenerator generator = new()
        {
            MaxT = 2,
            Velocity = 10,
            PositionFunction = time => time <= 1 ? 10 * time : 10 * (2 - time),
        };
        generator.SetPath([new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        List<Vector2> anchors = generator.Sliderate();

        // Assert
        anchors.Should().HaveCountGreaterThan(2);
        anchors[0].Should().Be(new Vector2(0, 0));
        anchors.Should().Contain(point => point.X >= 9);
        anchors[^1].X.Should().BeLessThanOrEqualTo(1);
        anchors.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }
}
