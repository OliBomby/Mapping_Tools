using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.Graph.Interpolation;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.Graph.Markers;
using Mapping_Tools.Core.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.Graph;

[TestClass]
public sealed class GraphStateTests
{
    [TestMethod]
    public void GetValue_WithParameterizedCurve_UsesAnchorTension()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0), new LinearInterpolator()),
            new GraphAnchor(new Vector2(1, 10), new SingleCurveInterpolator(), 0.5),
        ], 0, 0, 1, 10);

        // Act
        double value = state.GetValue(0.5);

        // Assert
        value.Should().BeApproximately(9.2414, 0.0001);
    }

    [TestMethod]
    public void GetIntegral_WithLinearGraph_MatchesAnalyticArea()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0), new LinearInterpolator()),
            new GraphAnchor(new Vector2(1, 10), new LinearInterpolator()),
        ], 0, 0, 1, 10);

        // Act
        double integral = state.GetIntegral(0, 1);

        // Assert
        integral.Should().BeApproximately(5, Precision.DoubleEpsilon);
        state.GetDerivative(0.25).Should().BeApproximately(10, Precision.DoubleEpsilon);
    }

    [TestMethod]
    public void GetMinIntegral_WithLinearSegmentCrossingZero_FindsInteriorMinimum()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, -1), new LinearInterpolator()),
            new GraphAnchor(new Vector2(1, 1), new LinearInterpolator()),
        ], 0, -1, 1, 1);

        // Act
        double minimum = state.GetMinIntegral();

        // Assert
        minimum.Should().BeApproximately(-0.25, 0.000001);
    }

    [TestMethod]
    public void EmptyAndSingleAnchorStates_ReturnSafeEvaluationValues()
    {
        // Arrange
        GraphState empty = new([], 0, 0, 1, 1);
        GraphState single = new([new GraphAnchor(new Vector2(0.25, 4))], 0, 0, 1, 5);

        // Act
        double emptyValue = empty.GetValue(0.5);
        double emptyDerivative = empty.GetDerivative(0.5);
        double emptyIntegral = empty.GetIntegral(0, 1);
        double singleValue = single.GetValue(0.5);
        double singleDerivative = single.GetDerivative(0.5);

        // Assert
        emptyValue.Should().Be(0);
        emptyDerivative.Should().Be(0);
        emptyIntegral.Should().Be(0);
        singleValue.Should().Be(4);
        singleDerivative.Should().Be(0);
    }

    [TestMethod]
    public void TextCodec_RoundTripsConstantAndCurveRepresentations()
    {
        // Arrange
        var constant = GraphStateTextCodec.CreateConstant(12.5);
        GraphState curve = new(
        [
            new GraphAnchor(new Vector2(0, 0), new LinearInterpolator()),
            new GraphAnchor(new Vector2(0.5, 2), new ParabolaInterpolator(), -0.25),
            new GraphAnchor(new Vector2(1, 1), new SingleCurveInterpolator(), 0.3),
        ], 0, 0, 1, 2);

        // Act
        string constantText = GraphStateTextCodec.Format(constant);
        string curveText = GraphStateTextCodec.Format(curve);
        bool constantParsed = GraphStateTextCodec.TryParse(constantText, out var parsedConstant);
        bool curveParsed = GraphStateTextCodec.TryParse(curveText, out var parsedCurve);

        // Assert
        constantText.Should().Be("12.5");
        constantParsed.Should().BeTrue();
        parsedConstant.GetValue(0.5).Should().Be(12.5);
        curveParsed.Should().BeTrue();
        parsedCurve.Anchors.Should().HaveCount(3);
        parsedCurve.Anchors[1].Tension.Should().BeApproximately(-0.25, Precision.DoubleEpsilon);
        parsedCurve.GetValue(0.5).Should().BeApproximately(curve.GetValue(0.5), 0.0001);
    }

    [TestMethod]
    public void TextCodec_WithMalformedAnchor_ReturnsValidationFailureAndDefaultState()
    {
        // Arrange
        const string text = "0:0:0|broken";

        // Act
        bool parsed = GraphStateTextCodec.TryParse(text, out var state);

        // Assert
        parsed.Should().BeFalse();
        state.Anchors.Should().HaveCount(2);
        state.GetValue(0.5).Should().Be(0.5);
    }

    [TestMethod]
    public void InterpolatorCatalog_PreservesLegacySelectionOrder()
    {
        // Arrange
        var types = GraphInterpolatorCatalog.GetInterpolators();

        // Act
        string[] names = types.Select(GraphInterpolatorCatalog.GetName).ToArray();

        // Assert
        names.Should().Equal(
            "Single curve", "Single curve 2", "Single curve 3", "Double curve",
            "Double curve 2", "Double curve 3", "Half sine", "Wave", "Parabola");
        GraphInterpolatorCatalog.GetInterpolatorIndex(typeof(LinearInterpolator)).Should().Be(-1);
    }

    [TestMethod]
    public void GraphStateClone_PreservesBuiltInInterpolatorTypeAndTension()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0)),
            new GraphAnchor(new Vector2(1, 1), new ParabolaInterpolator(), -0.4),
        ], 0, 0, 1, 1);

        // Act
        var clone = state.Clone();

        // Assert
        clone.Anchors[1].Interpolator.Should().BeOfType<ParabolaInterpolator>();
        clone.Anchors[1].Tension.Should().Be(-0.4);
        clone.Anchors.Should().NotBeSameAs(state.Anchors);
    }

    [TestMethod]
    public void GraphAnchor_SetInterpolator_PreservesTheExistingTensionParameter()
    {
        // Arrange
        GraphAnchor anchor = new(new Vector2(1, 1), new SingleCurveInterpolator(), 0.4);

        // Act
        anchor.Interpolator = new ParabolaInterpolator();

        // Assert
        anchor.Interpolator.P.Should().Be(0.4);
        anchor.Tension.Should().Be(0.4);
    }

    [TestMethod]
    public void MarkerGenerators_RespectBudgetAndSnapping()
    {
        // Arrange
        DoubleMarkerGenerator generator = new(0, 0.25, string.Empty, true);

        // Act
        var markers = generator.GenerateMarkers(0, 4, GraphMarkerOrientation.Vertical, 4).ToArray();

        // Assert
        markers.Should().HaveCountLessThanOrEqualTo(5);
        markers.Should().OnlyContain(marker => marker.Snappable);
        markers[0].Value.Should().Be(0);
        markers[0].Orientation.Should().Be(GraphMarkerOrientation.Vertical);
    }
}
