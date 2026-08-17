using Avalonia;
using Mapping_Tools.Application.ObjectVisualiser;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;
using Mapping_Tools.Desktop.Controls;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class ObjectVisualiserControlTests
{
    [TestMethod]
    public void FitToScene_WithCircle_MapsWorldCenterToViewportCenter()
    {
        // Arrange
        ObjectVisualiserObject circle = new(7, ObjectVisualiserObjectKind.Circle, new Vector2(100, 200), 10);
        ObjectVisualiserControl control = new() { Scene = new ObjectVisualiserScene([circle]) };
        control.Arrange(new Rect(0, 0, 200, 100));

        // Act
        control.FitToScene();

        // Assert
        Vector2 viewportPoint = control.CurrentTransform.WorldToViewport(circle.Position);
        viewportPoint.Should().Be(new Vector2(100, 50));
    }

    [TestMethod]
    public void HitTest_WithFittedCircle_ReturnsSceneObject()
    {
        // Arrange
        ObjectVisualiserObject circle = new(7, ObjectVisualiserObjectKind.Circle, new Vector2(100, 200), 10);
        ObjectVisualiserControl control = new() { Scene = new ObjectVisualiserScene([circle]) };
        control.Arrange(new Rect(0, 0, 200, 100));

        // Act
        var hit = control.HitTest(new Point(100, 50));

        // Assert
        hit.Should().NotBeNull();
        hit!.Object.Id.Should().Be(7);
        hit.Part.Should().Be(ObjectVisualiserHitPart.Body);
    }

    [TestMethod]
    public void DefaultVisualProperties_PreserveLegacySizingAndAnchorState()
    {
        // Arrange
        ObjectVisualiserControl control = new();

        // Act
        // Assert
        control.Thickness.Should().Be(40);
        control.BorderThickness.Should().Be(0.1);
        control.AnchorSize.Should().Be(0.2);
        control.ShowAnchors.Should().BeFalse();
        control.Progress.Should().Be(-1);
    }

    [TestMethod]
    public void FitToScene_WhenViewportResizes_KeepsContentCenteredInNewViewport()
    {
        // Arrange
        ObjectVisualiserObject circle = new(7, ObjectVisualiserObjectKind.Circle, new Vector2(100, 200), 10);
        ObjectVisualiserControl control = new() { Scene = new ObjectVisualiserScene([circle]) };
        control.Arrange(new Rect(0, 0, 200, 100));

        // Act
        control.Arrange(new Rect(0, 0, 400, 200));

        // Assert
        control.CurrentTransform.WorldToViewport(circle.Position).Should().Be(new Vector2(200, 100));
    }

    [TestMethod]
    public void PanBy_WhenViewportResizes_PreservesUserTransform()
    {
        // Arrange
        ObjectVisualiserObject circle = new(7, ObjectVisualiserObjectKind.Circle, new Vector2(100, 200), 10);
        ObjectVisualiserControl control = new() { Scene = new ObjectVisualiserScene([circle]) };
        control.Arrange(new Rect(0, 0, 200, 100));
        control.PanBy(new Vector2(10, 5));

        // Act
        control.Arrange(new Rect(0, 0, 400, 200));

        // Assert
        control.CurrentTransform.WorldToViewport(circle.Position).Should().Be(new Vector2(110, 55));
    }
}
