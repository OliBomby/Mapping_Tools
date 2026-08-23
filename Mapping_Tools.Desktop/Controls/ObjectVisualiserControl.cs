using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Draws one osu! hit object and its optional slider annotations inside the
///     available control bounds.
/// </summary>
public sealed class ObjectVisualiserControl : Control
{
    /// <summary>Maximum slider pixel length accepted by the legacy visualizer.</summary>
    public const double MAX_PIXEL_LENGTH = 1e6;

    /// <summary>Maximum number of calculated slider points accepted by the visualizer.</summary>
    public const double MAX_SEGMENT_COUNT = 1e6;

    /// <summary>Maximum number of slider anchors drawn by the visualizer.</summary>
    public const int MAX_ANCHOR_COUNT = 1500;

    /// <summary>Maximum number of source slider anchors accepted before path construction is skipped.</summary>
    public const int HARD_MAX_ANCHOR_COUNT = 5000;

    /// <summary>Identifies the hit object drawn by the control.</summary>
    public static readonly StyledProperty<HitObject?> HitObjectProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, HitObject?>(nameof(HitObject));

    /// <summary>Identifies the normalized slider-ball progress, or a negative value to hide it.</summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(Progress), -1);

    /// <summary>Identifies the optional pixel length used to rebuild a slider path.</summary>
    public static readonly StyledProperty<double?> CustomPixelLengthProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double?>(nameof(CustomPixelLength));

    /// <summary>Identifies whether slider anchors and their connector lines are drawn.</summary>
    public static readonly StyledProperty<bool> ShowAnchorsProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, bool>(nameof(ShowAnchors));

    /// <summary>Identifies the world-space diameter of circles and slider strokes.</summary>
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(Thickness), 40);

    /// <summary>Identifies the fractional outline thickness.</summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(BorderThickness), 0.1);

    /// <summary>Identifies the world-space size of slider anchor squares.</summary>
    public static readonly StyledProperty<double> AnchorSizeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(AnchorSize), 0.2);

    /// <summary>Identifies the object and slider-path fill brush.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(Fill));

    /// <summary>Identifies the object and slider-path outline brush.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(Stroke));

    /// <summary>Identifies the slider-ball outline brush.</summary>
    public static readonly StyledProperty<IBrush?> SliderBallStrokeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(SliderBallStroke));

    /// <summary>Identifies the extra markers drawn along a slider path.</summary>
    public static readonly StyledProperty<IReadOnlyList<ObjectVisualiserMarker>> ExtraMarkersProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IReadOnlyList<ObjectVisualiserMarker>>(
            nameof(ExtraMarkers), []);

    private Rect contentBounds = new(0, 0, 1, 1);
    private IReadOnlyList<Vector2> controlPoints = [];
    private IReadOnlyList<Vector2> pathPoints = [];
    private double scale = 1;

    private SliderPath? sliderPath;

    static ObjectVisualiserControl()
    {
        AffectsRender<ObjectVisualiserControl>(
            HitObjectProperty,
            ProgressProperty,
            CustomPixelLengthProperty,
            ShowAnchorsProperty,
            ThicknessProperty,
            BorderThicknessProperty,
            AnchorSizeProperty,
            FillProperty,
            StrokeProperty,
            SliderBallStrokeProperty,
            ExtraMarkersProperty);

        HitObjectProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.SetHitObject());
        CustomPixelLengthProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.SetHitObject());
        ThicknessProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.UpdateBounds());
        BorderThicknessProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.UpdateTransform());
    }

    /// <summary>Creates a clipped custom-drawn hit-object control.</summary>
    public ObjectVisualiserControl()
    {
        ClipToBounds = true;
        SizeChanged += (_, _) => UpdateTransform();
    }

    /// <summary>Gets or sets the hit object drawn by the control.</summary>
    public HitObject? HitObject
    {
        get => GetValue(HitObjectProperty);
        set => SetValue(HitObjectProperty, value);
    }

    /// <summary>Gets or sets the normalized slider-ball progress; values outside zero through one hide the ball.</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Gets or sets the optional slider pixel length used for previewing a rebuilt path.</summary>
    public double? CustomPixelLength
    {
        get => GetValue(CustomPixelLengthProperty);
        set => SetValue(CustomPixelLengthProperty, value);
    }

    /// <summary>Gets or sets whether slider anchors and connector lines are visible.</summary>
    public bool ShowAnchors
    {
        get => GetValue(ShowAnchorsProperty);
        set => SetValue(ShowAnchorsProperty, value);
    }

    /// <summary>Gets or sets the world-space diameter of circles and slider strokes.</summary>
    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>Gets or sets the fractional outline thickness.</summary>
    public double BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>Gets or sets the world-space size of slider anchor squares.</summary>
    public double AnchorSize
    {
        get => GetValue(AnchorSizeProperty);
        set => SetValue(AnchorSizeProperty, value);
    }

    /// <summary>Gets or sets the object and slider-path fill brush.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Gets or sets the object and slider-path outline brush.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>Gets or sets the slider-ball outline brush.</summary>
    public IBrush? SliderBallStroke
    {
        get => GetValue(SliderBallStrokeProperty);
        set => SetValue(SliderBallStrokeProperty, value);
    }

    /// <summary>Gets or sets the extra markers drawn along a slider path.</summary>
    public IReadOnlyList<ObjectVisualiserMarker> ExtraMarkers
    {
        get => GetValue(ExtraMarkersProperty);
        set => SetValue(ExtraMarkersProperty, value);
    }

    private double ThicknessWithoutOutline => (1 - BorderThickness) * Thickness;

    private double ThicknessInsideOutline => (1 - BorderThickness * 2) * Thickness;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 100 : availableSize.Height);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (HitObject is null) return;

        using IDisposable clip = context.PushClip(new Rect(Bounds.Size));
        if (HitObject.IsSlider && sliderPath is not null)
            DrawSlider(context);
        else if (HitObject.IsCircle) DrawCircle(context, Fill, GetOutlinePen(), HitObject.Pos, ThicknessWithoutOutline / 2);
    }

    private void SetHitObject()
    {
        sliderPath = null;
        controlPoints = [];
        pathPoints = [];

        if (HitObject is null)
        {
            contentBounds = new Rect(0, 0, 1, 1);
            UpdateTransform();
            return;
        }

        if (HitObject.IsSlider && HitObject.PixelLength < MAX_PIXEL_LENGTH && HitObject.CurvePoints is not null && HitObject.CurvePoints.Count < HARD_MAX_ANCHOR_COUNT)
            try
            {
                double? customLength = CustomPixelLength is { } value && double.IsFinite(value) && value >= 0
                    ? value
                    : null;
                var path = customLength is null
                    ? HitObject.GetSliderPath()
                    : new SliderPath(HitObject.SliderType, HitObject.GetAllCurvePoints().ToArray(), customLength);
                if (path.CalculatedPath.Count <= MAX_SEGMENT_COUNT)
                {
                    sliderPath = path;
                    controlPoints = path.ControlPoints.ToArray();
                    pathPoints = [HitObject.Pos, .. path.CalculatedPath];
                }
            }
            catch
            {
                // A malformed or extreme slider is simply not drawn.
            }

        UpdateBounds();
    }

    private void UpdateBounds()
    {
        if (HitObject is null)
        {
            contentBounds = new Rect(0, 0, 1, 1);
            UpdateTransform();
            return;
        }

        if (sliderPath is not null && pathPoints.Count > 0)
        {
            double left = pathPoints.Min(point => point.X);
            double top = pathPoints.Min(point => point.Y);
            double right = pathPoints.Max(point => point.X);
            double bottom = pathPoints.Max(point => point.Y);
            double padding = Thickness / 2;
            contentBounds = new Rect(left - padding, top - padding, right - left + padding * 2,
                bottom - top + padding * 2);
        }
        else
        {
            double padding = Math.Max(Thickness / 2, 0.5);
            contentBounds = new Rect(HitObject.Pos.X - padding, HitObject.Pos.Y - padding, padding * 2, padding * 2);
        }

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        scale = contentBounds.Width <= 0 || contentBounds.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0
            ? 1
            : Math.Min(Bounds.Width / contentBounds.Width, Bounds.Height / contentBounds.Height);
        InvalidateVisual();
    }

    private void DrawSlider(DrawingContext context)
    {
        var pathOutlinePen = Stroke is null ? null : new Pen(Stroke, Thickness * scale);
        var pathFillPen = Fill is null ? null : new Pen(Fill, ThicknessInsideOutline * scale);
        if (pathOutlinePen is not null) DrawPolyline(context, pathOutlinePen);

        if (pathFillPen is not null) DrawPolyline(context, pathFillPen);

        DrawCircleAtProgress(context, Fill, GetOutlinePen(), 0);
        DrawCircleAtProgress(context, Fill, GetOutlinePen(), 1);
        if (Progress is >= 0 and <= 1)
        {
            var sliderBallPen = SliderBallStroke is null
                ? null
                : new Pen(SliderBallStroke, Thickness * BorderThickness * scale);
            DrawCircleAtProgress(context, Fill, sliderBallPen, Progress);
        }

        if (ShowAnchors && controlPoints.Count <= MAX_ANCHOR_COUNT)
        {
            Pen connectorPen = new(Brushes.White, scale);
            Pen outlinePen = new(Brushes.Black, scale);
            for (int index = 0; index < controlPoints.Count - 1; index++) context.DrawLine(connectorPen, ToPoint(controlPoints[index]), ToPoint(controlPoints[index + 1]));

            for (int index = 0; index < controlPoints.Count; index++)
            {
                IBrush brush = index != 0 && controlPoints[index] == controlPoints[index - 1]
                    ? Brushes.Red
                    : Brushes.LightGray;
                DrawSquare(context, brush, outlinePen, controlPoints[index], AnchorSize);
            }
        }

        Pen markerOutlinePen = new(Brushes.Black, scale);
        foreach (var marker in ExtraMarkers)
            if (marker.Brush is not null && marker.Progress is >= 0 and <= 1)
                DrawSquare(context, marker.Brush, markerOutlinePen, sliderPath!.Value.PositionAt(marker.Progress), marker.Size);
    }

    private Pen? GetOutlinePen()
    {
        return Stroke is null ? null : new Pen(Stroke, Thickness * BorderThickness * scale);
    }

    private void DrawPolyline(DrawingContext context, Pen pen)
    {
        for (int index = 1; index < pathPoints.Count; index++) context.DrawLine(pen, ToPoint(pathPoints[index - 1]), ToPoint(pathPoints[index]));
    }

    private void DrawCircleAtProgress(DrawingContext context, IBrush? fill, Pen? pen, double progress)
    {
        DrawCircle(context, fill, pen, sliderPath!.Value.PositionAt(progress), ThicknessWithoutOutline / 2);
    }

    private void DrawCircle(DrawingContext context, IBrush? fill, Pen? pen, Vector2 position, double radius)
    {
        if (fill is null && pen is null) return;

        context.DrawEllipse(fill, pen, ToPoint(position), radius * scale, radius * scale);
    }

    private void DrawSquare(DrawingContext context, IBrush fill, Pen outline, Vector2 position, double size)
    {
        var point = ToPoint(position);
        double scaledSize = size * scale;
        context.DrawRectangle(fill, outline,
            new Rect(point.X - scaledSize / 2, point.Y - scaledSize / 2, scaledSize, scaledSize));
    }

    private Point ToPoint(Vector2 position)
    {
        return new Point(
            (position.X - contentBounds.Left) * scale,
            (position.Y - contentBounds.Top) * scale);
    }
}
