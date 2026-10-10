using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.MathUtil;

[TestClass]
public sealed class Box2Tests
{
    [TestMethod]
    public void Contains_WithReversedAxes_RespectsOpenAndClosedBoundaries()
    {
        // Arrange
        Box2 box = new(10, 20, 0, 0);

        // Act
        bool inside = box.Contains(new Vector2(5, 10), false);
        bool closedCorner = box.Contains(new Vector2(10, 20), true);
        bool openCorner = box.Contains(new Vector2(10, 20), false);
        bool outside = box.Contains(new Vector2(11, 10), true);

        // Assert
        inside.Should().BeTrue();
        closedCorner.Should().BeTrue();
        openCorner.Should().BeFalse();
        outside.Should().BeFalse();
    }

    [TestMethod]
    public void Contains_WithNormalAxes_ExcludesEachBoundaryOnlyForOpenRegion()
    {
        // Arrange
        Box2 box = new(0, 0, 10, 20);
        Vector2[] boundaryPoints =
        [
            new(0, 10), new(10, 10), new(5, 0), new(5, 20),
        ];

        // Act
        bool[] closed = boundaryPoints.Select(point => box.Contains(point, true)).ToArray();
        bool[] open = boundaryPoints.Select(point => box.Contains(point, false)).ToArray();

        // Assert
        closed.Should().OnlyContain(value => value);
        open.Should().OnlyContain(value => !value);
        box.Contains(new Vector2(5, 10), false).Should().BeTrue();
    }

    [TestMethod]
    public void Contains_WithZeroWidth_ContainsBoundaryOnlyWhenClosed()
    {
        // Arrange
        Box2 box = new(5, 0, 5, 10);
        Vector2 point = new(5, 5);

        // Act
        bool closed = box.Contains(point, true);
        bool open = box.Contains(point, false);

        // Assert
        closed.Should().BeTrue();
        open.Should().BeFalse();
    }

    [TestMethod]
    public void FromDimensions_WithNegativeSize_KeepsAbsoluteDimensionsAndReversedBounds()
    {
        // Arrange
        Vector2 position = new(10, 20);
        Vector2 size = new(-4, -6);

        // Act
        Box2 box = Box2.FromDimensions(position, size);

        // Assert
        box.Left.Should().Be(10);
        box.Top.Should().Be(20);
        box.Right.Should().Be(6);
        box.Bottom.Should().Be(14);
        box.Width.Should().Be(4);
        box.Height.Should().Be(6);
    }

    [TestMethod]
    public void Translated_WithOffset_DoesNotChangeOriginalBox()
    {
        // Arrange
        Box2 box = new(1, 2, 3, 4);

        // Act
        Box2 translated = box.Translated(new Vector2(-2, 5));

        // Assert
        translated.Should().Be(new Box2(-1, 7, 1, 9));
        box.Should().Be(new Box2(1, 2, 3, 4));
    }

    [TestMethod]
    public void Equals_WithOneDifferentBoundary_ReturnsFalse()
    {
        // Arrange
        Box2 original = new(1, 2, 3, 4);
        Box2[] changed =
        [
            new(0, 2, 3, 4), new(1, 0, 3, 4),
            new(1, 2, 0, 4), new(1, 2, 3, 0),
        ];

        // Act
        bool[] equal = [.. changed.Select(box => original == box)];

        // Assert
        equal.Should().OnlyContain(value => !value);
    }
}
