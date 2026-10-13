using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Mapping_Tools.Infrastructure.Tests.Tools.GeometryDashboard;

[TestClass]
public sealed class WindowsGeometryDashboardOverlayHostTests
{
    [TestMethod]
    [DataRow(1.0)]
    [DataRow(1.25)]
    [DataRow(1.5)]
    [DataRow(2.0)]
    public void DrawFrame_WithDisplayScaling_RendersShapesAtPhysicalEditorCoordinates(double scaling)
    {
        // Arrange
        var window = new GeometryDashboardWindow(
            new PlatformWindowId(42), 7, "map.osu", new Box2(-1920, 100, 0, 1180),
            true, true, new Vector2(scaling, scaling), true)
        {
            ClientBounds = new Box2(-1920, 100, 0, 1180),
        };
        WindowsGeometryDashboardCoordinateTransform transform = new(
            window, null,
            new WindowsGeometryDashboardOsuDisplaySettings(new Vector2(1920, 1080), true, false, Vector2.Zero),
            new Box2());
        var bounds = transform.EditorBox;
        using SKBitmap bitmap = new(1920, 1080);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        GeometryDashboardOverlayScene scene = new(
        [
            Shape(GeometryDashboardOverlayShapeKind.Point, new Vector2(256, 192), default, 5),
            Shape(GeometryDashboardOverlayShapeKind.Line, new Vector2(128, 96), new Vector2(384, 96), 0),
            Shape(GeometryDashboardOverlayShapeKind.Circle, new Vector2(256, 288), default, 24),
            Shape(GeometryDashboardOverlayShapeKind.Box, new Vector2(128, 240), new Vector2(192, 300), 0),
        ]);
        var point = toBitmapCoordinate(new Vector2(256, 192));
        var line = toBitmapCoordinate(new Vector2(256, 96));
        var circleEdge = toBitmapCoordinate(new Vector2(280, 288));
        var boxEdge = toBitmapCoordinate(new Vector2(128, 270));

        // Act
        WindowsGeometryDashboardOverlayHost.DrawFrame(canvas, scene, transform, bounds);

        // Assert
        bitmap.GetPixel((int)Math.Round(point.X + 5), (int)Math.Round(point.Y)).Alpha.Should().BeGreaterThan(0);
        bitmap.GetPixel((int)Math.Round(line.X), (int)Math.Round(line.Y)).Alpha.Should().BeGreaterThan(0);
        bitmap.GetPixel((int)Math.Round(circleEdge.X), (int)Math.Round(circleEdge.Y)).Alpha.Should().BeGreaterThan(0);
        bitmap.GetPixel((int)Math.Round(boxEdge.X), (int)Math.Round(boxEdge.Y)).Alpha.Should().BeGreaterThan(0);

        Vector2 toBitmapCoordinate(Vector2 coordinate)
        {
            // Cursor positions and overlay pixels must use the same physical grid.
            return transform.EditorToScreenCoordinate(coordinate)
                   - new Vector2(bounds.Left + 0.5, bounds.Top + 0.5);
        }
    }

    private static GeometryDashboardOverlayShape Shape(
        GeometryDashboardOverlayShapeKind kind, Vector2 start, Vector2 end, double radius)
    {
        return new GeometryDashboardOverlayShape(
            kind, start, end, radius, RgbaColour.FromRgb(255, 255, 255), 1, 3, DashStylesEnum.Solid);
    }
}
