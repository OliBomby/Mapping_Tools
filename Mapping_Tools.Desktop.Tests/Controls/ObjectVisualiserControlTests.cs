using Avalonia;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class ObjectVisualiserControlTests
{
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
        control.HitObject.Should().BeNull();
        control.ExtraMarkers.Should().BeEmpty();
    }

    [TestMethod]
    public void HitObjectProperty_AcceptsOneDomainObjectWithoutSceneState()
    {
        // Arrange
        HitObject slider = new("100,200,0,2,0,L|200:200,1,100");
        ObjectVisualiserControl control = new();

        // Act
        control.HitObject = slider;
        control.CustomPixelLength = 200;
        control.Arrange(new Rect(0, 0, 200, 100));

        // Assert
        control.HitObject.Should().BeSameAs(slider);
        control.CustomPixelLength.Should().Be(200);
    }

    [TestMethod]
    public void LegacyLimits_ExposeTheOriginalPathSafetyValues()
    {
        // Arrange
        // Act
        // Assert
        ObjectVisualiserControl.MAX_PIXEL_LENGTH.Should().Be(1e6);
        ObjectVisualiserControl.MAX_SEGMENT_COUNT.Should().Be(1e6);
        ObjectVisualiserControl.MAX_ANCHOR_COUNT.Should().Be(1500);
        ObjectVisualiserControl.HARD_MAX_ANCHOR_COUNT.Should().Be(5000);
    }
}
