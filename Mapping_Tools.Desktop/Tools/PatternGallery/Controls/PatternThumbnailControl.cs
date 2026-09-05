using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Controls;

/// <summary>Draws the small pattern thumbnail used by Pattern Gallery.</summary>
public sealed class PatternThumbnailControl : Control
{
    private const int thumbnail_margin = 10;
    private const int maximum_object_count = 100;
    private const double maximum_pixel_length = 1e6;
    private const int maximum_anchor_count = 5000;

    /// <summary>Identifies the beatmap represented by the thumbnail.</summary>
    public static readonly StyledProperty<Beatmap?> BeatmapProperty =
        AvaloniaProperty.Register<PatternThumbnailControl, Beatmap?>(nameof(Beatmap));

    /// <summary>Identifies the slider and circle interior brush.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<PatternThumbnailControl, IBrush?>(nameof(Fill));

    /// <summary>Identifies the object outline and spinner brush.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<PatternThumbnailControl, IBrush?>(nameof(Stroke));

    static PatternThumbnailControl()
    {
        AffectsRender<PatternThumbnailControl>(BeatmapProperty, FillProperty, StrokeProperty);
    }

    /// <summary>Creates a clipped custom-drawn pattern thumbnail.</summary>
    public PatternThumbnailControl()
    {
        ClipToBounds = true;
    }

    /// <summary>Gets or sets the beatmap represented by the thumbnail.</summary>
    public Beatmap? Beatmap
    {
        get => GetValue(BeatmapProperty);
        set => SetValue(BeatmapProperty, value);
    }

    /// <summary>Gets or sets the slider and circle interior brush.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Gets or sets the object outline and spinner brush.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Beatmap is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double scale = Math.Min(
            Math.Max(0, Bounds.Width - thumbnail_margin * 2) / 512,
            Math.Max(0, Bounds.Height - thumbnail_margin * 2) / 384);
        if (scale <= 0) return;

        double offsetX = (Bounds.Width - 512 * scale) / 2;
        double offsetY = (Bounds.Height - 384 * scale) / 2;
        double radius = Beatmap.GetHitObjectRadius(Beatmap.Difficulty["CircleSize"].DoubleValue);
        var sliderPaths = BuildSliderPaths();
        HitObject? next = null;

        foreach (var hitObject in Beatmap.HitObjects.Take(maximum_object_count).Reverse())
        {
            if (next is null)
            {
                next = hitObject;
                continue;
            }

            if (!next.ActualNewCombo && Vector2.Distance(next.Pos, hitObject.EndPos) > radius * 2.5)
            {
                double distance = Vector2.Distance(next.Pos, hitObject.EndPos);
                var start = Vector2.Lerp(next.Pos, hitObject.EndPos, radius / distance * 1.2);
                var end = Vector2.Lerp(next.Pos, hitObject.EndPos, 1 - radius / distance * 1.2);
                DrawLine(context, start, end, radius * 0.1, scale, offsetX, offsetY);
            }

            DrawHitObject(context, next, radius, scale, offsetX, offsetY, sliderPaths);
            next = hitObject;
        }

        if (next is not null) DrawHitObject(context, next, radius, scale, offsetX, offsetY, sliderPaths);
    }

    private Dictionary<HitObject, SliderPath> BuildSliderPaths()
    {
        Dictionary<HitObject, SliderPath> paths = [];
        foreach (var hitObject in Beatmap!.HitObjects.Take(maximum_object_count))
        {
            if (!hitObject.IsSlider || hitObject.PixelLength >= maximum_pixel_length || hitObject.CurvePoints is null || hitObject.CurvePoints.Count >= maximum_anchor_count)
                continue;

            try
            {
                var path = hitObject.GetSliderPath();
                paths[hitObject] = path;
                hitObject.EndPos = path.PositionAt(1);
            }
            catch
            {
                // Invalid slider paths do not prevent the remaining thumbnail from rendering.
            }
        }

        return paths;
    }

    private void DrawHitObject(
        DrawingContext context,
        HitObject hitObject,
        double radius,
        double scale,
        double offsetX,
        double offsetY,
        IReadOnlyDictionary<HitObject, SliderPath> sliderPaths)
    {
        var position = hitObject.StackedPos;
        if (hitObject.IsSlider)
        {
            if (!sliderPaths.TryGetValue(hitObject, out var path)) return;

            var shift = position - hitObject.Pos;
            Vector2[] pathPoints = path.CalculatedPath.Select(point => point + shift).ToArray();
            if (pathPoints.Length > 0)
            {
                StreamGeometry pathGeometry = CreatePathGeometry(pathPoints);
                using (context.PushTransform(GetThumbnailTransform(scale, offsetX, offsetY)))
                {
                    context.DrawGeometry(null,
                        new Pen(Stroke ?? Brushes.White, radius * 1.95,
                            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round),
                        pathGeometry);
                    context.DrawGeometry(null,
                        new Pen(Fill ?? Brushes.DarkSlateGray, radius * 1.65,
                            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round),
                        pathGeometry);
                }
            }

            DrawFilledCircle(context, path.PositionAt(0) + shift, radius, scale, offsetX, offsetY);
            DrawComboNumber(context, hitObject, radius, scale, offsetX, offsetY);
        }
        else if (hitObject.IsSpinner)
        {
            DrawRing(context, new Vector2(256, 192), 150, radius * 0.15, scale, offsetX, offsetY);
            DrawRing(context, new Vector2(256, 192), 5, radius * 0.15, scale, offsetX, offsetY);
        }
        else if (hitObject.IsCircle)
        {
            DrawFilledCircle(context, position, radius, scale, offsetX, offsetY);
            DrawComboNumber(context, hitObject, radius, scale, offsetX, offsetY);
        }
    }

    private void DrawFilledCircle(
        DrawingContext context,
        Vector2 position,
        double radius,
        double scale,
        double offsetX,
        double offsetY)
    {
        var point = ToPoint(position, scale, offsetX, offsetY);
        context.DrawEllipse(Stroke ?? Brushes.White, null, point, radius * scale, radius * scale);
        context.DrawEllipse(Fill ?? Brushes.Green, null, point, radius * 0.846 * scale, radius * 0.846 * scale);
    }

    private void DrawRing(
        DrawingContext context,
        Vector2 position,
        double radius,
        double thickness,
        double scale,
        double offsetX,
        double offsetY)
    {
        context.DrawEllipse(null, new Pen(Stroke ?? Brushes.White, thickness * scale),
            ToPoint(position, scale, offsetX, offsetY), radius * scale, radius * scale);
    }

    private void DrawComboNumber(
        DrawingContext context,
        HitObject hitObject,
        double radius,
        double scale,
        double offsetX,
        double offsetY)
    {
        FormattedText text = new(
            hitObject.ComboIndex.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            radius * 0.6 * scale,
            Brushes.White);
        var center = ToPoint(hitObject.StackedPos, scale, offsetX, offsetY);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private static StreamGeometry CreatePathGeometry(IReadOnlyList<Vector2> points)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(points[0].X, points[0].Y), false);
            for (int index = 1; index < points.Count; index++)
                geometryContext.LineTo(new Point(points[index].X, points[index].Y));

            geometryContext.EndFigure(false);
        }

        return geometry;
    }

    private static Matrix GetThumbnailTransform(double scale, double offsetX, double offsetY)
    {
        return Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY);
    }

    private static void DrawLine(
        DrawingContext context,
        Vector2 start,
        Vector2 end,
        double thickness,
        double scale,
        double offsetX,
        double offsetY)
    {
        context.DrawLine(new Pen(Brushes.White, thickness * scale),
            ToPoint(start, scale, offsetX, offsetY),
            ToPoint(end, scale, offsetX, offsetY));
    }

    private static Point ToPoint(Vector2 point, double scale, double offsetX, double offsetY)
    {
        return new Point(offsetX + point.X * scale, offsetY + point.Y * scale);
    }
}
