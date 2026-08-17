using Mapping_Tools.Application.ObjectVisualiser;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.ObjectVisualiser;

[TestClass]
public sealed class ObjectVisualiserHitTesterTests
{
    [TestMethod]
    public void FromHitObjects_WithCircleAndSlider_PreservesObjectOrderAndYAxis()
    {
        // Arrange
        HitObject circle = new("100,200,0,1,0,0:0:0:0:");
        HitObject slider = new("100,300,100,2,0,L|200:300,1,100");

        // Act
        ObjectVisualiserScene scene = ObjectVisualiserSceneBuilder.FromHitObjects([circle, slider], 4);

        // Assert
        scene.Objects.Should().HaveCount(2);
        scene.Objects[0].Kind.Should().Be(ObjectVisualiserObjectKind.Circle);
        scene.Objects[0].Position.Should().Be(new Vector2(100, 200));
        scene.Objects[1].Kind.Should().Be(ObjectVisualiserObjectKind.Slider);
        scene.Objects[1].Path.Should().NotBeNull();
        scene.Objects[1].Position.Y.Should().BeGreaterThan(scene.Objects[0].Position.Y);
    }

    [TestMethod]
    public void FromHitObjects_WithComboMetadata_PreservesLabelsAndComboBoundaries()
    {
        // Arrange
        HitObject first = new("100,200,0,1,0,0:0:0:0:")
        {
            ComboIndex = 3,
            ActualNewCombo = true
        };
        HitObject second = new("100,300,100,1,0,0:0:0:0:")
        {
            ComboIndex = 4,
            ActualNewCombo = false
        };

        // Act
        ObjectVisualiserScene scene = ObjectVisualiserSceneBuilder.FromHitObjects([first, second], 4);

        // Assert
        scene.Objects.Select(item => item.ComboIndex).Should().Equal(3, 4);
        scene.Objects.Select(item => item.StartsCombo).Should().Equal(true, false);
    }

    [TestMethod]
    public void HitTest_WithOverlappingObjects_ReturnsFrontMostObject()
    {
        // Arrange
        ObjectVisualiserObject back = new(1, ObjectVisualiserObjectKind.Circle, new Vector2(10, 10), 5);
        ObjectVisualiserObject front = new(2, ObjectVisualiserObjectKind.Circle, new Vector2(10, 10), 5);
        ObjectVisualiserScene scene = new([back, front]);
        ObjectVisualiserTransform transform = ObjectVisualiserTransform.Identity;

        // Act
        ObjectVisualiserHit? hit = ObjectVisualiserHitTester.HitTest(scene, transform, new Vector2(10, 10), 0);

        // Assert
        hit.Should().NotBeNull();
        hit!.Object.Should().BeSameAs(front);
        hit.Part.Should().Be(ObjectVisualiserHitPart.Body);
    }

    [TestMethod]
    public void HitTest_WithVisibleSliderAnchor_ReturnsAnchorAndIndex()
    {
        // Arrange
        ObjectVisualiserPath path = new([new Vector2(0, 0), new Vector2(100, 0)]);
        ObjectVisualiserObject slider = new(
            1,
            ObjectVisualiserObjectKind.Slider,
            new Vector2(0, 0),
            5,
            path,
            [new Vector2(0, 0), new Vector2(100, 0)]);
        ObjectVisualiserScene scene = new([slider]);

        // Act
        ObjectVisualiserHit? hit = ObjectVisualiserHitTester.HitTest(
            scene,
            ObjectVisualiserTransform.Identity,
            new Vector2(100, 0),
            0,
            showAnchors: true);

        // Assert
        hit.Should().NotBeNull();
        hit!.Part.Should().Be(ObjectVisualiserHitPart.Anchor);
        hit.AnchorIndex.Should().Be(1);
    }

    [TestMethod]
    public void HitTest_WithAnchorsOnCircle_DoesNotTreatThemAsVisibleAnchors()
    {
        // Arrange
        ObjectVisualiserObject circle = new(
            1,
            ObjectVisualiserObjectKind.Circle,
            new Vector2(0, 0),
            5,
            anchors: [new Vector2(100, 100)]);
        ObjectVisualiserScene scene = new([circle]);

        // Act
        ObjectVisualiserHit? hit = ObjectVisualiserHitTester.HitTest(
            scene,
            ObjectVisualiserTransform.Identity,
            new Vector2(100, 100),
            0,
            showAnchors: true);

        // Assert
        hit.Should().BeNull();
    }

    [TestMethod]
    public void HitTest_WithSpinnerCenterRing_ReturnsBody()
    {
        // Arrange
        ObjectVisualiserObject spinner = new(1, ObjectVisualiserObjectKind.Spinner, new Vector2(50, 50), 150);
        ObjectVisualiserScene scene = new([spinner]);

        // Act
        ObjectVisualiserHit? hit = ObjectVisualiserHitTester.HitTest(
            scene,
            ObjectVisualiserTransform.Identity,
            new Vector2(55, 50),
            0);

        // Assert
        hit.Should().NotBeNull();
        hit!.Part.Should().Be(ObjectVisualiserHitPart.Body);
    }

    [TestMethod]
    public void FromHitObjects_WithCustomPixelLength_RebuildsSliderPathToRequestedLength()
    {
        // Arrange
        HitObject slider = new("100,300,100,2,0,L|200:300,1,100");

        // Act
        ObjectVisualiserScene scene = ObjectVisualiserSceneBuilder.FromHitObjects(
            [slider],
            4,
            customPixelLength: 200);

        // Assert
        scene.Objects[0].Path.Should().NotBeNull();
        scene.Objects[0].Path!.Length.Should().BeApproximately(200, 0.001);
    }
}
