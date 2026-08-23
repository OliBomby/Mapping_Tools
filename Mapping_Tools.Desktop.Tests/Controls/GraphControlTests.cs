using Avalonia;
using Avalonia.Input;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.Graph.Markers;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class GraphControlTests
{
    [TestMethod]
    public void DefaultState_UsesCenteredUnitAnchors()
    {
        // Arrange
        GraphControl control = new();

        // Act
        var state = control.GetGraphState();

        // Assert
        state.Anchors.Should().HaveCount(2);
        state.Anchors.Select(anchor => anchor.Pos).Should().Equal(new Vector2(0, 0.5), new Vector2(1, 0.5));
        state.MinY.Should().Be(0);
        state.MaxY.Should().Be(1);
    }

    [TestMethod]
    public void MoveAnchor_ClampsEdgesLocksAxesAndRaisesStateChange()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0)),
            new GraphAnchor(new Vector2(0.4, 0.2), new SingleCurveInterpolator()),
            new GraphAnchor(new Vector2(1, 1)),
        ], 0, 0, 1, 1);
        GraphControl control = new() { GraphState = state };
        GraphState? changedState = null;
        control.StateChanged += (_, args) => changedState = args.State;

        // Act
        bool changed = control.MoveAnchor(1, new Vector2(0.9, 0.8), KeyModifiers.Shift);

        // Assert
        changed.Should().BeTrue();
        changedState.Should().NotBeNull();
        changedState!.Anchors[1].Pos.Should().Be(new Vector2(0.9, 0.2));
        control.GraphState!.Anchors[1].Pos.Should().Be(new Vector2(0.9, 0.2));
    }

    [TestMethod]
    public void MoveAnchor_UsesSnappableMarkersUnlessAltIsPressed()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0)),
            new GraphAnchor(new Vector2(0.5, 0.5), new SingleCurveInterpolator()),
            new GraphAnchor(new Vector2(1, 1)),
        ], 0, 0, 1, 1);
        GraphControl control = new()
        {
            GraphState = state,
            SnapX = true,
            Markers = [new GraphMarker { Orientation = GraphMarkerOrientation.Vertical, Value = 0.5, Snappable = true }],
        };

        // Act
        control.MoveAnchor(1, new Vector2(0.49, 0.6));
        double snapped = control.GraphState!.Anchors[1].Pos.X;
        control.MoveAnchor(1, new Vector2(0.49, 0.6), KeyModifiers.Alt);

        // Assert
        snapped.Should().Be(0.5);
        control.GraphState!.Anchors[1].Pos.X.Should().BeApproximately(0.49, 0.001);
    }

    [TestMethod]
    public void MoveAnchor_WithInfiniteMarkerRangeSnapsToTheNearestMarker()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0)),
            new GraphAnchor(new Vector2(0.1, 0.5), new SingleCurveInterpolator()),
            new GraphAnchor(new Vector2(1, 1)),
        ], 0, 0, 1, 1);
        GraphControl control = new()
        {
            GraphState = state,
            SnapX = true,
            Markers = [new GraphMarker { Orientation = GraphMarkerOrientation.Vertical, Value = 0.9, Snappable = true }],
        };

        // Act
        control.MoveAnchor(1, new Vector2(0.1, 0.5));

        // Assert
        control.GraphState!.Anchors[1].Pos.X.Should().Be(0.9);
    }

    [TestMethod]
    public void MoveAnchor_WithLockedEdgeRetainsItsExistingCoordinate()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0.2, 0.1)),
            new GraphAnchor(new Vector2(0.8, 0.9)),
        ], 0, 0, 1, 1);
        GraphControl control = new() { GraphState = state };

        // Act
        control.MoveAnchor(0, new Vector2(0.7, 0.4));

        // Assert
        control.GraphState!.Anchors[0].Pos.Should().Be(new Vector2(0.2, 0.4));
    }

    [TestMethod]
    public void ResetTension_WithParameterizedAnchor_ReturnsToZero()
    {
        // Arrange
        GraphState state = new(
        [
            new GraphAnchor(new Vector2(0, 0)),
            new GraphAnchor(new Vector2(1, 1), new SingleCurveInterpolator(), 0.75),
        ], 0, 0, 1, 1);
        GraphControl control = new() { GraphState = state };

        // Act
        bool changed = control.ResetTension(1);

        // Assert
        changed.Should().BeTrue();
        control.GraphState!.Anchors[1].Tension.Should().Be(0);
    }

    [TestMethod]
    public void PanBy_WithZoomedViewportStaysInsideGraphBounds()
    {
        // Arrange
        GraphControl control = new() { GraphState = GraphState.CreateDefault() };
        control.Arrange(new Rect(0, 0, 400, 200));
        control.ViewMinX = 0.25;
        control.ViewMaxX = 0.75;
        control.ViewMinY = 0.25;
        control.ViewMaxY = 0.75;

        // Act
        control.PanBy(new Vector2(1000, -1000));

        // Assert
        control.ViewMinX.Should().Be(0.5);
        control.ViewMaxX.Should().Be(1);
        control.ViewMinY.Should().Be(0.5);
        control.ViewMaxY.Should().Be(1);
    }

    [TestMethod]
    public void AddAndRemoveAnchor_PreserveNonEdgeDeletionRule()
    {
        // Arrange
        GraphControl control = new() { GraphState = GraphState.CreateDefault() };

        // Act
        int index = control.AddAnchor(new Vector2(0.5, 0.25));
        bool removedMiddle = control.RemoveAnchor(index);
        bool removedFirst = control.RemoveAnchor(0);
        bool removedLast = control.RemoveAnchor(control.GraphState!.Anchors.Count - 1);

        // Assert
        index.Should().Be(1);
        removedMiddle.Should().BeTrue();
        removedFirst.Should().BeFalse();
        removedLast.Should().BeFalse();
        control.GraphState.Anchors.Should().HaveCount(2);
    }

    [TestMethod]
    public void ViewMapping_AndZoomStayInvertibleAroundFocus()
    {
        // Arrange
        GraphControl control = new() { GraphState = GraphState.CreateDefault() };
        control.Arrange(new Rect(0, 0, 400, 200));
        Point focus = new(120, 80);
        var before = control.GetGraphPosition(focus);

        // Act
        control.ZoomAt(focus, 2);

        // Assert
        control.GetGraphPosition(focus).Should().Be(before);
    }

    [TestMethod]
    public void WheelZoomPosition_UsesLegacyZeroLowerBoundsEvenWhenGraphMinimumIsNegative()
    {
        // Arrange
        GraphState state = new(
            [new GraphAnchor(new Vector2(0, -1)), new GraphAnchor(new Vector2(4, 2))],
            0,
            -1,
            4,
            2);

        // Act
        bool negativeYAllowed = GraphControl.IsWheelZoomPositionInLegacyBounds(new Vector2(2, -0.1f), state);
        bool insideAllowed = GraphControl.IsWheelZoomPositionInLegacyBounds(new Vector2(2, 0.1f), state);

        // Assert
        negativeYAllowed.Should().BeFalse();
        insideAllowed.Should().BeTrue();
    }

    [TestMethod]
    public void BoundsChange_WithScalingEnabled_TransformsAnchorCoordinatesAndResetsView()
    {
        // Arrange
        GraphState state = new(
            [new GraphAnchor(new Vector2(0.25, 0.5)), new GraphAnchor(new Vector2(0.75, 1))],
            0,
            0,
            1,
            1);
        GraphControl control = new() { GraphState = state };
        control.Arrange(new Rect(0, 0, 400, 200));
        control.ViewMinX = 0.2;
        control.ViewMaxX = 0.8;

        // Act
        control.MaxX = 2;

        // Assert
        control.GraphState!.Anchors[0].Pos.X.Should().Be(0.5);
        control.GraphState.Anchors[1].Pos.X.Should().Be(1.5);
        control.ViewMinX.Should().Be(0);
        control.ViewMaxX.Should().Be(2);
    }
}
