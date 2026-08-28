using System.Globalization;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class GraphStateToDoubleConverterTests
{
    [TestMethod]
    public void Convert_NonConstantGraph_ReturnsItsAverageValue()
    {
        // Arrange
        GraphStateToDoubleConverter converter = new();
        GraphState state = new(
            [
                new GraphAnchor(new Vector2(0, 3), new LinearInterpolator()),
                new GraphAnchor(new Vector2(1, 5), new LinearInterpolator()),
            ],
            0,
            0,
            1,
            5);

        // Act
        double value = (double)converter.Convert(state, typeof(double), null, CultureInfo.InvariantCulture);

        // Assert
        value.Should().BeApproximately(4, Precision.DOUBLE_EPSILON);
    }

    [TestMethod]
    public void ConvertBack_NonFiniteValue_ReturnsNull()
    {
        // Arrange
        GraphStateToDoubleConverter converter = new();

        // Act
        object result = converter.ConvertBack(double.NaN, typeof(GraphState), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void ConvertBack_FiniteValue_ReturnsConstantGraphState()
    {
        // Arrange
        GraphStateToDoubleConverter converter = new();

        // Act
        object result = converter.ConvertBack(2.5, typeof(GraphState), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<GraphState>();
        ((GraphState)result).Anchors.Select(anchor => anchor.Pos.Y).Should().Equal(2.5, 2.5);
    }
}
