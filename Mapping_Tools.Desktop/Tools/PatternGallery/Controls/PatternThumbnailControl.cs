using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Controls;

/// <summary>Draws the small pattern thumbnail used by Pattern Gallery.</summary>
public sealed class PatternThumbnailControl : Control
{
    private const int thumbnail_margin = 10;
    private const int maximum_object_count = 100;
    private const double maximum_pixel_length = 1e6;
    private const int maximum_anchor_count = 5000;

    private static readonly IBrush circleInsideBrush = Brushes.Green;
    private static readonly IBrush circleOutsideBrush = Brushes.White;
    private static readonly IBrush sliderInsideBrush = Brushes.DarkSlateGray;
    private static readonly IBrush sliderOutsideBrush = Brushes.White;
    private static readonly IBrush comboTextBrush = Brushes.White;
    private static readonly IBrush spinnerBrush = Brushes.White;
    private static readonly IBrush followPointBrush = Brushes.White;

    /// <summary>Identifies the beatmap represented by the thumbnail.</summary>
    public static readonly StyledProperty<Beatmap?> BeatmapProperty =
        AvaloniaProperty.Register<PatternThumbnailControl, Beatmap?>(nameof(Beatmap));

    /// <summary>Identifies the background-prepared slider path points used by the thumbnail.</summary>
    public static readonly StyledProperty<IReadOnlyDictionary<HitObject, IReadOnlyList<Vector2>>?> SliderPathPointsProperty =
        AvaloniaProperty.Register<PatternThumbnailControl, IReadOnlyDictionary<HitObject, IReadOnlyList<Vector2>>?>(
            nameof(SliderPathPoints));

    private readonly Dictionary<HitObject, StreamGeometry> geometryCache = [];
    private CancellationTokenSource? preparationCancellation;

    static PatternThumbnailControl()
    {
        AffectsRender<PatternThumbnailControl>(BeatmapProperty);
        AffectsRender<PatternThumbnailControl>(SliderPathPointsProperty);
        BeatmapProperty.Changed.AddClassHandler<PatternThumbnailControl>(static (control, _) => control.PrepareBeatmap());
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

    /// <summary>Gets or sets the immutable slider path data prepared off the UI thread.</summary>
    public IReadOnlyDictionary<HitObject, IReadOnlyList<Vector2>>? SliderPathPoints
    {
        get => GetValue(SliderPathPointsProperty);
        private set => SetValue(SliderPathPointsProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        if (Beatmap is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double scale = Math.Min(
            Math.Max(0, Bounds.Width - thumbnail_margin * 2) / 512,
            Math.Max(0, Bounds.Height - thumbnail_margin * 2) / 384);
        if (scale <= 0) return;

        double offsetX = (Bounds.Width - 512 * scale) / 2;
        double offsetY = (Bounds.Height - 384 * scale) / 2;
        double radius = Beatmap.GetHitObjectRadius(Beatmap.Difficulty["CircleSize"].DoubleValue);
        var sliderPaths = SliderPathPoints;
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

    private void PrepareBeatmap()
    {
        preparationCancellation?.Cancel();
        preparationCancellation?.Dispose();
        geometryCache.Clear();

        var beatmap = Beatmap;
        if (beatmap is null)
        {
            SliderPathPoints = null;
            return;
        }

        CancellationTokenSource cancellation = new();
        preparationCancellation = cancellation;
        _ = PrepareBeatmapAsync(beatmap, cancellation.Token, cancellation);
    }

    private async Task PrepareBeatmapAsync(
        Beatmap beatmap,
        CancellationToken token,
        CancellationTokenSource cancellation)
    {
        try
        {
            var paths = await Task.Run(
                    () => BuildSliderPathPoints(beatmap, token),
                    token)
                .ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested || !ReferenceEquals(Beatmap, beatmap)) return;

                foreach (var path in paths)
                    if (path.Value.Count > 0)
                        path.Key.EndPos = path.Value[^1];

                SliderPathPoints = paths;
                geometryCache.Clear();
                InvalidateVisual();
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(preparationCancellation, cancellation)) preparationCancellation = null;
            cancellation.Dispose();
        }
    }

    private static Dictionary<HitObject, IReadOnlyList<Vector2>> BuildSliderPathPoints(
        Beatmap beatmap,
        CancellationToken cancellationToken)
    {
        Dictionary<HitObject, IReadOnlyList<Vector2>> paths = [];
        foreach (var hitObject in beatmap.HitObjects.Take(maximum_object_count))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!hitObject.IsSlider || hitObject.PixelLength >= maximum_pixel_length || hitObject.CurvePoints is null || hitObject.CurvePoints.Count >= maximum_anchor_count)
                continue;

            try
            {
                var path = hitObject.GetSliderPath();
                paths[hitObject] = path.CalculatedPath.ToArray();
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
        IReadOnlyDictionary<HitObject, IReadOnlyList<Vector2>>? sliderPaths)
    {
        var position = hitObject.StackedPos;
        if (hitObject.IsSlider)
        {
            var shift = position - hitObject.Pos;
            if (sliderPaths is not null && sliderPaths.TryGetValue(hitObject, out var pathPoints) && pathPoints.Count > 0)
            {
                if (!geometryCache.TryGetValue(hitObject, out var pathGeometry))
                {
                    pathGeometry = CreatePathGeometry(pathPoints.Select(point => point + shift).ToArray());
                    geometryCache[hitObject] = pathGeometry;
                }

                using (context.PushTransform(GetThumbnailTransform(scale, offsetX, offsetY)))
                {
                    context.DrawGeometry(null,
                        new Pen(sliderOutsideBrush, radius * 1.95,
                            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round),
                        pathGeometry);
                    context.DrawGeometry(null,
                        new Pen(sliderInsideBrush, radius * 1.65,
                            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round),
                        pathGeometry);
                }

                DrawFilledCircle(context, pathPoints[0] + shift, radius, scale, offsetX, offsetY);
            }
            else
            {
                // Render the head while path preparation is pending, and keep
                // malformed/degenerate one-object sliders visible as circles.
                DrawFilledCircle(context, position, radius, scale, offsetX, offsetY);
            }

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
        context.DrawEllipse(circleOutsideBrush, null, point, radius * scale, radius * scale);
        context.DrawEllipse(circleInsideBrush, null, point, radius * 0.846 * scale, radius * 0.846 * scale);
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
        context.DrawEllipse(null, new Pen(spinnerBrush, thickness * scale),
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
            comboTextBrush);
        var center = ToPoint(hitObject.StackedPos, scale, offsetX, offsetY);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private static StreamGeometry CreatePathGeometry(IReadOnlyList<Vector2> points)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
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
        context.DrawLine(new Pen(followPointBrush, thickness * scale),
            ToPoint(start, scale, offsetX, offsetY),
            ToPoint(end, scale, offsetX, offsetY));
    }

    private static Point ToPoint(Vector2 point, double scale, double offsetX, double offsetY)
    {
        return new Point(offsetX + point.X * scale, offsetY + point.Y * scale);
    }
}
