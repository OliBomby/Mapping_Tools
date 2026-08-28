using System.Globalization;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class GraphStateTextConverterTests
{
    [TestMethod]
    public void Convert_CurveGraph_PreservesGraphTextInsteadOfReturningScalar()
    {
        // Arrange
        GraphStateTextConverter converter = new();
        GraphState state = new(
            [
                new GraphAnchor(new Vector2(0, 0), new SingleCurveInterpolator()),
                new GraphAnchor(new Vector2(1, 2), new SingleCurveInterpolator(), 0.5),
            ],
            0,
            0,
            1,
            2);

        // Act
        string text = (string)converter.Convert(state, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        text.Should().Contain("0:0:");
        text.Should().Contain("1:2:");
        text.Should().Contain("|");
    }

    [TestMethod]
    public void ConvertBack_CurveText_RestoresAnchorsAndBounds()
    {
        // Arrange
        GraphStateTextConverter converter = new();

        // Act
        object result = converter.ConvertBack(
            "0:0:0:0|1:2:0:0.5",
            typeof(GraphState),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<GraphState>();
        var state = (GraphState)result;
        state.Anchors.Select(anchor => anchor.Pos.Y).Should().Equal(0, 2);
        state.MaxY.Should().Be(2);
    }
}
