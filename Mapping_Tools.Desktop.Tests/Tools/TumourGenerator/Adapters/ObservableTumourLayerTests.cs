using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TumourGenerator.Adapters;

[TestClass]
public sealed class ObservableTumourLayerTests
{
    [TestMethod]
    public void ScalarProjection_RefreshingFromGraph_DoesNotReplaceGraph()
    {
        // Arrange
        TumourLayer model = TumourLayer.GetDefaultLayer();
        model.TumourScale = new GraphState(
            [new GraphAnchor(new Vector2(0, 10)), new GraphAnchor(new Vector2(0.5, 30)), new GraphAnchor(new Vector2(1, 20))],
            0,
            0,
            1,
            40);
        ObservableTumourLayer layer = new(model);
        double scalar = layer.TumourScaleValue;

        // Act
        layer.TumourScaleValue = scalar;

        // Assert
        layer.TumourScale.Anchors.Select(anchor => anchor.Pos.Y).Should().Equal(10, 30, 20);
    }
}
