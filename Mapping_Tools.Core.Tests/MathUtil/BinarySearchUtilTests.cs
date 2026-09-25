using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.MathUtil;

[TestClass]
public sealed class BinarySearchUtilTests
{
    [TestMethod]
    public void BinarySearch_WithDuplicateKeys_SelectsRequestedEndOfRun()
    {
        // Arrange
        int[] values = [1, 3, 3, 3, 5];

        // Act
        int left = BinarySearchUtil.BinarySearch(values, 3, value => value, BinarySearchUtil.EqualitySelection.Leftmost);
        int right = BinarySearchUtil.BinarySearch(values, 3, value => value, BinarySearchUtil.EqualitySelection.Rightmost);

        // Assert
        left.Should().Be(1);
        right.Should().Be(3);
    }

    [DataRow(0, -1)]
    [DataRow(2, -2)]
    [DataRow(4, -5)]
    [DataRow(6, -6)]
    [TestMethod]
    public void BinarySearch_WithMissingKey_ReturnsComplementOfInsertionIndex(int key, int expected)
    {
        // Arrange
        int[] values = [1, 3, 3, 3, 5];

        // Act
        int index = BinarySearchUtil.BinarySearch(values, key, value => value);

        // Assert
        index.Should().Be(expected);
    }

    [TestMethod]
    public void BinarySearch_WithEmptyCollection_ReturnsFirstInsertionIndex()
    {
        // Arrange
        int[] values = [];

        // Act
        int index = BinarySearchUtil.BinarySearch(values, 10, value => value);

        // Assert
        index.Should().Be(-1);
    }

    [TestMethod]
    public void BinarySearch_WithMissingKeyAndDuplicateSelection_ReturnsInsertionComplement()
    {
        // Arrange
        int[] values = [1, 3, 3, 5];

        // Act
        int left = BinarySearchUtil.BinarySearch(values, 4, value => value, BinarySearchUtil.EqualitySelection.Leftmost);
        int right = BinarySearchUtil.BinarySearch(values, 4, value => value, BinarySearchUtil.EqualitySelection.Rightmost);

        // Assert
        left.Should().Be(-4);
        right.Should().Be(-4);
    }

    [TestMethod]
    public void DoubleBinarySearch_WithReversedBounds_FindsLastAcceptedValue()
    {
        // Arrange
        const double boundary = 2.5;

        // Act
        double result = BinarySearchUtil.DoubleBinarySearch(10, 0, 0.001, value => value <= boundary);

        // Assert
        result.Should().BeLessThanOrEqualTo(boundary);
        result.Should().BeGreaterThan(boundary - 0.001);
    }

    [TestMethod]
    public void DoubleBinarySearch_WithNoAcceptedValues_ReturnsOriginalLowerBound()
    {
        // Arrange
        const double lower = 0;

        // Act
        double result = BinarySearchUtil.DoubleBinarySearch(lower, 10, 0.001, _ => false);

        // Assert
        result.Should().Be(lower);
    }

    [TestMethod]
    public void DoubleBinarySearch_WithAllValuesAccepted_ReturnsUpperBound()
    {
        // Arrange
        const double upper = 10;

        // Act
        double result = BinarySearchUtil.DoubleBinarySearch(0, upper, 0.001, _ => true);

        // Assert
        result.Should().Be(upper);
    }

    [TestMethod]
    public void DoubleBinarySearch_WithIntervalExactlyAtTolerance_DoesNotBisect()
    {
        // Arrange
        const double epsilon = 1;

        // Act
        double result = BinarySearchUtil.DoubleBinarySearch(0, 1, epsilon, value => value <= 0.5);

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public void Vector2BinarySearch_WithDiagonalSegment_StopsWithinEuclideanTolerance()
    {
        // Arrange
        Vector2 lower = new(0, 0);
        Vector2 upper = new(10, 10);
        Vector2 boundary = new(4, 4);

        // Act
        Vector2 result = BinarySearchUtil.Vector2BinarySearch(lower, upper, 0.01, value => value.X <= boundary.X);

        // Assert
        result.X.Should().BeLessThanOrEqualTo(4);
        Vector2.Distance(result, boundary).Should().BeLessThan(0.01);
    }
}
