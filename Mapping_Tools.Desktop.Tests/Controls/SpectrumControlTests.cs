using Avalonia;
using Mapping_Tools.Core.Spectrum;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class SpectrumControlTests
{
    [TestMethod]
    public void EmptyFrame_DoesNotCreateBars()
    {
        // Arrange
        SpectrumControl control = new() { Frame = new SpectrumFrame(44_100, 8, []) };
        control.Arrange(new Rect(0, 0, 200, 64));

        // Act
        IReadOnlyList<Rect> bars = SpectrumControl.CalculateBarRects(
            control.Frame,
            control.Bounds.Size,
            control.VerticalScale,
            control.MinimumBarWidth);

        // Assert
        bars.Should().BeEmpty();
    }

    [TestMethod]
    public void FrameUpdate_UsesPeakForVerticalScaling()
    {
        // Arrange
        SpectrumFrame frame = new(44_100, 8, [0.25, 1, 0.5]);
        SpectrumControl control = new() { Frame = frame };
        control.Arrange(new Rect(0, 0, 300, 100));

        // Act
        IReadOnlyList<Rect> bars = SpectrumControl.CalculateBarRects(frame, control.Bounds.Size, 1, 1);

        // Assert
        bars.Should().HaveCount(3);
        bars[0].Height.Should().BeApproximately(25, 0.001);
        bars[1].Height.Should().BeApproximately(100, 0.001);
        bars[2].Height.Should().BeApproximately(50, 0.001);
    }

    [TestMethod]
    public void Resize_RecomputesBarWidthAndKeepsBarsBottomAligned()
    {
        // Arrange
        SpectrumFrame frame = new(44_100, 8, [1, 0.5]);
        SpectrumControl control = new() { Frame = frame };
        control.Arrange(new Rect(0, 0, 100, 50));
        IReadOnlyList<Rect> original = SpectrumControl.CalculateBarRects(frame, control.Bounds.Size, 1, 1);

        // Act
        control.Arrange(new Rect(0, 0, 200, 80));
        IReadOnlyList<Rect> resized = SpectrumControl.CalculateBarRects(frame, control.Bounds.Size, 1, 1);

        // Assert
        resized[0].Width.Should().BeGreaterThan(original[0].Width);
        resized[0].Bottom.Should().Be(80);
        resized[1].Bottom.Should().Be(80);
    }

    [TestMethod]
    public void DenseFrame_GroupsBinsWithoutDrawingOutsideTheViewport()
    {
        // Arrange
        SpectrumFrame frame = new(44_100, 256, Enumerable.Repeat(1d, 100));

        // Act
        IReadOnlyList<Rect> bars = SpectrumControl.CalculateBarRects(frame, new Size(10, 20), 1, 2);

        // Assert
        bars.Should().HaveCount(5);
        bars.Should().AllSatisfy(bar =>
        {
            bar.Left.Should().BeGreaterThanOrEqualTo(0);
            bar.Right.Should().BeLessThanOrEqualTo(10);
            bar.Bottom.Should().Be(20);
        });
    }

    [TestMethod]
    public void NullFrameOrInvalidScale_DoesNotCreateBars()
    {
        // Arrange
        SpectrumFrame frame = new(44_100, 8, [1]);
        Size size = new(100, 20);

        // Act
        IReadOnlyList<Rect> nullFrameBars = SpectrumControl.CalculateBarRects(null, size, 1, 1);
        IReadOnlyList<Rect> invalidScaleBars = SpectrumControl.CalculateBarRects(frame, size, double.NaN, 1);

        // Assert
        nullFrameBars.Should().BeEmpty();
        invalidScaleBars.Should().BeEmpty();
    }
}
