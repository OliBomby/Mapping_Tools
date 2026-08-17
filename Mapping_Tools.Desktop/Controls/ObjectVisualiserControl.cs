using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Globalization;
using System.Collections.Specialized;
using Mapping_Tools.Application.ObjectVisualiser;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Provides the Desktop rendering and pointer boundary for reusable object-visualizer scenes.</summary>
public sealed class ObjectVisualiserControl : Control
{
    /// <summary>Matches the legacy control's maximum anchor count for drawing.</summary>
    public const int MaxAnchorCount = ObjectVisualiserHitTester.MaxAnchorCount;

    /// <summary>Matches the legacy control's hard maximum anchor count.</summary>
    public const int HardMaxAnchorCount = ObjectVisualiserSceneBuilder.HardMaxAnchorCount;

    private const double HitTolerance = 6;
    private ObjectVisualiserTransform transform = ObjectVisualiserTransform.Identity;
    private bool fitToScene = true;
    private IPointer? panPointer;
    private Point panStart;
    private ObjectVisualiserTransform panTransform;
    private ObjectVisualiserHit? hoveredHit;
    private INotifyCollectionChanged? markersCollection;
    private bool isAttached;

    /// <summary>Identifies the framework-neutral scene drawn by the control.</summary>
    public static readonly StyledProperty<ObjectVisualiserScene?> SceneProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, ObjectVisualiserScene?>(nameof(Scene));

    /// <summary>Identifies the normalized slider progress used for the slider ball.</summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(Progress), -1);

    /// <summary>Identifies whether slider anchors and their connecting lines are drawn.</summary>
    public static readonly StyledProperty<bool> ShowAnchorsProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, bool>(nameof(ShowAnchors));

    /// <summary>Identifies the world-space diameter of circles and slider strokes.</summary>
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(Thickness), 40);

    /// <summary>Identifies the fractional outline thickness.</summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(BorderThickness), 0.1);

    /// <summary>Identifies the world-space size multiplier for anchors.</summary>
    public static readonly StyledProperty<double> AnchorSizeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(AnchorSize), 0.2);

    /// <summary>Identifies the object fill brush.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(Fill));

    /// <summary>Identifies the object outline brush.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(Stroke));

    /// <summary>Identifies the slider-ball outline brush.</summary>
    public static readonly StyledProperty<IBrush?> SliderBallStrokeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(SliderBallStroke));

    /// <summary>Identifies the optional selected-object outline brush.</summary>
    public static readonly StyledProperty<IBrush?> SelectedBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(SelectedBrush));

    /// <summary>Identifies the optional hovered-object outline brush.</summary>
    public static readonly StyledProperty<IBrush?> HoveredBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(HoveredBrush));

    /// <summary>Identifies the anchor connector brush.</summary>
    public static readonly StyledProperty<IBrush?> AnchorLineBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(AnchorLineBrush), Brushes.White);

    /// <summary>Identifies the normal anchor fill brush.</summary>
    public static readonly StyledProperty<IBrush?> AnchorBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(AnchorBrush), Brushes.LightGray);

    /// <summary>Identifies the duplicate-anchor fill brush.</summary>
    public static readonly StyledProperty<IBrush?> DuplicateAnchorBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(DuplicateAnchorBrush), Brushes.Red);

    /// <summary>Identifies the anchor outline brush.</summary>
    public static readonly StyledProperty<IBrush?> AnchorOutlineBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(AnchorOutlineBrush), Brushes.Black);

    /// <summary>Identifies the extra marker collection.</summary>
    public static readonly StyledProperty<IReadOnlyList<ObjectVisualiserMarker>> MarkersProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IReadOnlyList<ObjectVisualiserMarker>>(
            nameof(Markers), []);

    /// <summary>Identifies whether combo numbers are drawn at object starts.</summary>
    public static readonly StyledProperty<bool> ShowComboNumbersProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, bool>(nameof(ShowComboNumbers));

    /// <summary>Identifies whether follow lines are drawn between nearby objects.</summary>
    public static readonly StyledProperty<bool> ShowFollowLinesProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, bool>(nameof(ShowFollowLines));

    /// <summary>Identifies the brush used for combo labels.</summary>
    public static readonly StyledProperty<IBrush?> ComboNumberBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(ComboNumberBrush), Brushes.White);

    /// <summary>Identifies the brush used for follow lines.</summary>
    public static readonly StyledProperty<IBrush?> FollowLineBrushProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, IBrush?>(nameof(FollowLineBrush), Brushes.White);

    /// <summary>Identifies the world-space combo-number font size.</summary>
    public static readonly StyledProperty<double> ComboNumberSizeProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, double>(nameof(ComboNumberSize), 24);

    /// <summary>Identifies the selected object identifier.</summary>
    public static readonly StyledProperty<int?> SelectedObjectIdProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, int?>(nameof(SelectedObjectId));

    /// <summary>Identifies the hovered object identifier.</summary>
    public static readonly StyledProperty<int?> HoveredObjectIdProperty =
        AvaloniaProperty.Register<ObjectVisualiserControl, int?>(nameof(HoveredObjectId));

    static ObjectVisualiserControl()
    {
        AffectsRender<ObjectVisualiserControl>(
            SceneProperty,
            ProgressProperty,
            ShowAnchorsProperty,
            ThicknessProperty,
            BorderThicknessProperty,
            AnchorSizeProperty,
            FillProperty,
            StrokeProperty,
            SliderBallStrokeProperty,
            SelectedBrushProperty,
            HoveredBrushProperty,
            AnchorLineBrushProperty,
            AnchorBrushProperty,
            DuplicateAnchorBrushProperty,
            AnchorOutlineBrushProperty,
            MarkersProperty,
            ShowComboNumbersProperty,
            ShowFollowLinesProperty,
            ComboNumberBrushProperty,
            FollowLineBrushProperty,
            ComboNumberSizeProperty,
            SelectedObjectIdProperty,
            HoveredObjectIdProperty);

        SceneProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.SceneChanged());
        MarkersProperty.Changed.AddClassHandler<ObjectVisualiserControl>((control, _) => control.MarkersChanged());
    }

    /// <summary>Creates a focusable visualizer control with bounds clipping enabled.</summary>
    public ObjectVisualiserControl()
    {
        Focusable = true;
        ClipToBounds = true;
        SizeChanged += (_, _) =>
        {
            if (fitToScene)
            {
                FitToScene();
            }
        };
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        base.OnAttachedToVisualTree(eventArgs);
        isAttached = true;
        MarkersChanged();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        if (panPointer is not null)
        {
            panPointer.Capture(null);
            panPointer = null;
        }

        isAttached = false;
        MarkersChanged();
        base.OnDetachedFromVisualTree(eventArgs);
    }

    /// <summary>Raised when a left click changes selection, including clearing it outside all objects.</summary>
    public event EventHandler<ObjectVisualiserHitEventArgs>? ObjectSelected;

    /// <summary>Raised when the hovered object or anchor changes.</summary>
    public event EventHandler<ObjectVisualiserHitEventArgs>? ObjectHovered;

    /// <summary>Gets or sets the framework-neutral scene.</summary>
    public ObjectVisualiserScene? Scene { get => GetValue(SceneProperty); set => SetValue(SceneProperty, value); }

    /// <summary>Gets or sets the normalized slider progress; values outside zero through one hide the ball.</summary>
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    /// <summary>Gets or sets whether slider anchors are drawn and hit-tested.</summary>
    public bool ShowAnchors { get => GetValue(ShowAnchorsProperty); set => SetValue(ShowAnchorsProperty, value); }

    /// <summary>Gets or sets the world-space diameter of circles and slider strokes.</summary>
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>Gets or sets the fractional outline thickness.</summary>
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    /// <summary>Gets or sets the world-space size multiplier for anchors.</summary>
    public double AnchorSize { get => GetValue(AnchorSizeProperty); set => SetValue(AnchorSizeProperty, value); }

    /// <summary>Gets or sets the object fill brush.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>Gets or sets the object outline brush.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>Gets or sets the slider-ball outline brush.</summary>
    public IBrush? SliderBallStroke { get => GetValue(SliderBallStrokeProperty); set => SetValue(SliderBallStrokeProperty, value); }

    /// <summary>Gets or sets the optional selected-object outline brush.</summary>
    public IBrush? SelectedBrush { get => GetValue(SelectedBrushProperty); set => SetValue(SelectedBrushProperty, value); }

    /// <summary>Gets or sets the optional hovered-object outline brush.</summary>
    public IBrush? HoveredBrush { get => GetValue(HoveredBrushProperty); set => SetValue(HoveredBrushProperty, value); }

    /// <summary>Gets or sets the anchor connector brush.</summary>
    public IBrush? AnchorLineBrush { get => GetValue(AnchorLineBrushProperty); set => SetValue(AnchorLineBrushProperty, value); }

    /// <summary>Gets or sets the normal anchor fill brush.</summary>
    public IBrush? AnchorBrush { get => GetValue(AnchorBrushProperty); set => SetValue(AnchorBrushProperty, value); }

    /// <summary>Gets or sets the duplicate-anchor fill brush.</summary>
    public IBrush? DuplicateAnchorBrush { get => GetValue(DuplicateAnchorBrushProperty); set => SetValue(DuplicateAnchorBrushProperty, value); }

    /// <summary>Gets or sets the anchor outline brush.</summary>
    public IBrush? AnchorOutlineBrush { get => GetValue(AnchorOutlineBrushProperty); set => SetValue(AnchorOutlineBrushProperty, value); }

    /// <summary>Gets or sets extra square markers drawn on slider paths.</summary>
    public IReadOnlyList<ObjectVisualiserMarker> Markers { get => GetValue(MarkersProperty); set => SetValue(MarkersProperty, value); }

    /// <summary>Gets or sets whether combo numbers are rendered at object starts.</summary>
    public bool ShowComboNumbers { get => GetValue(ShowComboNumbersProperty); set => SetValue(ShowComboNumbersProperty, value); }

    /// <summary>Gets or sets whether follow lines are rendered between nearby objects.</summary>
    public bool ShowFollowLines { get => GetValue(ShowFollowLinesProperty); set => SetValue(ShowFollowLinesProperty, value); }

    /// <summary>Gets or sets the combo-number text brush.</summary>
    public IBrush? ComboNumberBrush { get => GetValue(ComboNumberBrushProperty); set => SetValue(ComboNumberBrushProperty, value); }

    /// <summary>Gets or sets the follow-line brush.</summary>
    public IBrush? FollowLineBrush { get => GetValue(FollowLineBrushProperty); set => SetValue(FollowLineBrushProperty, value); }

    /// <summary>Gets or sets the combo-number font size in world units.</summary>
    public double ComboNumberSize { get => GetValue(ComboNumberSizeProperty); set => SetValue(ComboNumberSizeProperty, value); }

    /// <summary>Gets or sets the selected object's stable identifier.</summary>
    public int? SelectedObjectId { get => GetValue(SelectedObjectIdProperty); set => SetValue(SelectedObjectIdProperty, value); }

    /// <summary>Gets or sets the hovered object's stable identifier.</summary>
    public int? HoveredObjectId { get => GetValue(HoveredObjectIdProperty); set => SetValue(HoveredObjectIdProperty, value); }

    /// <summary>Gets the current framework-neutral viewport transform.</summary>
    public ObjectVisualiserTransform CurrentTransform => transform;

    /// <summary>Fits the complete scene into the current control bounds.</summary>
    public void FitToScene()
    {
        fitToScene = true;
        if (Scene is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            transform = ObjectVisualiserTransform.Identity;
        }
        else
        {
            transform = ObjectVisualiserTransform.Fit(
                Scene.ContentBounds.Inflate(Thickness / 2, Thickness / 2),
                new Vector2(Bounds.Width, Bounds.Height));
        }

        InvalidateVisual();
    }

    /// <summary>Pans the scene by a viewport-pixel delta.</summary>
    /// <param name="viewportDelta">The pixel delta to apply.</param>
    public void PanBy(Vector2 viewportDelta)
    {
        EnsureTransform();
        transform = transform.PanBy(viewportDelta);
        fitToScene = false;
        InvalidateVisual();
    }

    /// <summary>Zooms the scene around a viewport point.</summary>
    /// <param name="viewportPoint">The fixed point in control coordinates.</param>
    /// <param name="factor">The positive multiplicative zoom factor.</param>
    public void ZoomAt(Point viewportPoint, double factor)
    {
        EnsureTransform();
        transform = transform.ZoomAt(new Vector2(viewportPoint.X, viewportPoint.Y), factor);
        fitToScene = false;
        InvalidateVisual();
    }

    /// <summary>Returns the object or anchor under a viewport point.</summary>
    /// <param name="point">The point in control coordinates.</param>
    /// <returns>The hit result, or <see langword="null"/>.</returns>
    public ObjectVisualiserHit? HitTest(Point point)
    {
        EnsureTransform();
        return Scene is null
            ? null
            : ObjectVisualiserHitTester.HitTest(
                Scene,
                transform,
                new Vector2(point.X, point.Y),
                HitTolerance,
                ShowAnchors,
                AnchorSize,
                Thickness / 2);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        EnsureTransform();
        if (Scene is null)
        {
            return;
        }

        using IDisposable clip = context.PushClip(new Rect(Bounds.Size));
        if (ShowFollowLines && FollowLineBrush is not null)
        {
            IReadOnlyList<ObjectVisualiserObject> objects = Scene.Objects.Take(100).ToArray();
            for (int index = 1; index < objects.Count; index++)
            {
                ObjectVisualiserObject previous = objects[index - 1];
                ObjectVisualiserObject current = objects[index];
                if (current.StartsCombo)
                {
                    continue;
                }

                double distance = Vector2.Distance(previous.EndPosition, current.Position);
                if (distance <= previous.Radius * 2.5)
                {
                    continue;
                }

                Vector2 start = Vector2.Lerp(previous.EndPosition, current.Position, previous.Radius / distance * 1.2);
                Vector2 end = Vector2.Lerp(previous.EndPosition, current.Position, 1 - previous.Radius / distance * 1.2);
                context.DrawLine(new Pen(FollowLineBrush, Math.Max(1, previous.Radius * 0.1 * transform.Scale)),
                    ToPoint(start), ToPoint(end));
            }
        }

        foreach (ObjectVisualiserObject visualObject in Scene.Objects)
        {
            DrawObject(context, visualObject);
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width,
        double.IsInfinity(availableSize.Height) ? 100 : availableSize.Height);

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        Point point = eventArgs.GetPosition(this);
        if (panPointer == eventArgs.Pointer)
        {
            Vector2 delta = new(point.X - panStart.X, point.Y - panStart.Y);
            transform = panTransform.PanBy(delta);
            fitToScene = false;
            InvalidateVisual();
            return;
        }

        SetHovered(HitTest(point));
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs eventArgs)
    {
        base.OnPointerExited(eventArgs);
        if (panPointer is null)
        {
            SetHovered(null);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        PointerPoint point = eventArgs.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed)
        {
            SetHovered(null);
            panPointer = eventArgs.Pointer;
            panStart = point.Position;
            panTransform = transform;
            eventArgs.Pointer.Capture(this);
            eventArgs.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        ObjectVisualiserHit? hit = HitTest(point.Position);
        SetCurrentValue(SelectedObjectIdProperty, hit?.Object.Id);
        if (hit is not null)
        {
            eventArgs.Handled = true;
        }

        ObjectSelected?.Invoke(this, new ObjectVisualiserHitEventArgs(hit));
        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs eventArgs)
    {
        base.OnPointerReleased(eventArgs);
        if (panPointer == eventArgs.Pointer)
        {
            eventArgs.Pointer.Capture(null);
            panPointer = null;
            SetHovered(null);
            eventArgs.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs eventArgs)
    {
        base.OnPointerCaptureLost(eventArgs);
        if (panPointer == eventArgs.Pointer)
        {
            panPointer = null;
            SetHovered(null);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs eventArgs)
    {
        base.OnPointerWheelChanged(eventArgs);
        EnsureTransform();
        double factor = Math.Pow(1.1, eventArgs.Delta.Y);
        if (double.IsFinite(factor) && factor > 0)
        {
            Point point = eventArgs.GetPosition(this);
            ZoomAt(point, factor);
            eventArgs.Handled = true;
        }
    }

    private void SceneChanged()
    {
        fitToScene = true;
        SetCurrentValue(SelectedObjectIdProperty, null);
        FitToScene();
        if (hoveredHit is not null)
        {
            SetHovered(null);
        }
        else
        {
            SetCurrentValue(HoveredObjectIdProperty, null);
        }
    }

    private void EnsureTransform()
    {
        if (fitToScene && Scene is not null && Bounds.Width > 0 && Bounds.Height > 0)
        {
            FitToScene();
        }
    }

    private void SetHovered(ObjectVisualiserHit? hit)
    {
        int? id = hit?.Object.Id;
        if (HitsEqual(hoveredHit, hit))
        {
            return;
        }

        hoveredHit = hit;
        SetCurrentValue(HoveredObjectIdProperty, id);
        ObjectHovered?.Invoke(this, new ObjectVisualiserHitEventArgs(hit));
        InvalidateVisual();
    }

    private void MarkersChanged()
    {
        if (markersCollection is not null)
        {
            markersCollection.CollectionChanged -= OnMarkersCollectionChanged;
        }

        markersCollection = isAttached ? Markers as INotifyCollectionChanged : null;
        if (markersCollection is not null)
        {
            markersCollection.CollectionChanged += OnMarkersCollectionChanged;
        }

        InvalidateVisual();
    }

    private void OnMarkersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs) => InvalidateVisual();

    private static bool HitsEqual(ObjectVisualiserHit? left, ObjectVisualiserHit? right) =>
        left is null && right is null ||
        left is not null && right is not null &&
        left.Object.Id == right.Object.Id && left.Part == right.Part && left.AnchorIndex == right.AnchorIndex;

    private void DrawObject(DrawingContext context, ObjectVisualiserObject visualObject)
    {
        IBrush? outlineBrush = GetOutlineBrush(visualObject);
        double scale = transform.Scale;
        double outlineWidth = Thickness * BorderThickness * scale;
        double insideWidth = (1 - BorderThickness * 2) * Thickness * scale;
        double radius = (1 - BorderThickness) * Thickness * scale / 2;
        Pen? outlinePen = outlineBrush is null ? null : new Pen(outlineBrush, outlineWidth);

        if (visualObject.Kind == ObjectVisualiserObjectKind.Slider && visualObject.Path is not null)
        {
            if (Fill is not null)
            {
                DrawPolyline(context, visualObject.Path.Points, new Pen(Fill, insideWidth));
                if (outlinePen is not null)
                {
                    DrawPolyline(context, visualObject.Path.Points, new Pen(outlineBrush, Thickness * scale));
                }

                DrawCircleAtProgress(context, visualObject.Path, 0, radius, Fill, outlinePen);
                DrawCircleAtProgress(context, visualObject.Path, 1, radius, Fill, outlinePen);
                if (Progress is >= 0 and <= 1)
                {
                    Pen? ballPen = SliderBallStroke is null ? null : new Pen(SliderBallStroke, outlineWidth);
                    DrawCircleAtProgress(context, visualObject.Path, Progress, radius, Fill, ballPen);
                }
            }

            if (ShowAnchors && visualObject.Anchors.Count <= MaxAnchorCount)
            {
                for (var i = 0; i < visualObject.Anchors.Count - 1; i++)
                {
                    DrawLine(context, AnchorLineBrush, visualObject.Anchors[i], visualObject.Anchors[i + 1], scale);
                }

                for (var i = 0; i < visualObject.Anchors.Count; i++)
                {
                    IBrush? brush = i > 0 && visualObject.Anchors[i] == visualObject.Anchors[i - 1]
                        ? DuplicateAnchorBrush
                        : AnchorBrush;
                    DrawSquare(context, brush, AnchorOutlineBrush, visualObject.Anchors[i], AnchorSize * scale);
                }
            }

            foreach (ObjectVisualiserMarker marker in Markers ?? [])
            {
                if (marker.Progress is >= 0 and <= 1 && marker.Brush is not null)
                {
                    DrawSquare(context, marker.Brush, AnchorOutlineBrush,
                        visualObject.Path.PositionAt(marker.Progress), marker.Size * scale);
                }
            }
        }
        else if (visualObject.Kind == ObjectVisualiserObjectKind.Spinner)
        {
            Point center = ToPoint(visualObject.Position);
            context.DrawEllipse(null, outlinePen, center, visualObject.Radius * scale, visualObject.Radius * scale);
            context.DrawEllipse(null, outlinePen, center, ObjectVisualiserHitTester.SpinnerCenterRadius * scale,
                ObjectVisualiserHitTester.SpinnerCenterRadius * scale);
        }
        else if (visualObject.Kind == ObjectVisualiserObjectKind.Circle)
        {
            DrawCircle(context, Fill, outlinePen, visualObject.Position, radius);
        }

        if (ShowComboNumbers && ComboNumberBrush is not null && visualObject.Kind != ObjectVisualiserObjectKind.Spinner)
        {
            DrawComboNumber(context, visualObject);
        }
    }

    private void DrawComboNumber(DrawingContext context, ObjectVisualiserObject visualObject)
    {
        double size = Math.Max(1, ComboNumberSize * transform.Scale);
        FormattedText text = new(
            visualObject.ComboIndex.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            size,
            ComboNumberBrush);
        Vector2 center = transform.WorldToViewport(visualObject.Position);
        context.DrawText(text, new Point(
            center.X - text.Width / 2,
            center.Y - text.Height / 2));
    }

    private IBrush? GetOutlineBrush(ObjectVisualiserObject visualObject) =>
        SelectedObjectId == visualObject.Id && SelectedBrush is not null ? SelectedBrush :
        HoveredObjectId == visualObject.Id && HoveredBrush is not null ? HoveredBrush : Stroke;

    private void DrawPolyline(DrawingContext context, IReadOnlyList<Vector2> points, Pen pen)
    {
        for (var i = 1; i < points.Count; i++)
        {
            DrawLine(context, pen.Brush, points[i - 1], points[i], pen.Thickness);
        }
    }

    private void DrawCircleAtProgress(DrawingContext context, ObjectVisualiserPath path, double progress,
        double radius, IBrush? fill, Pen? pen) =>
        DrawCircle(context, fill, pen, path.PositionAt(progress), radius);

    private void DrawCircle(DrawingContext context, IBrush? fill, Pen? pen, Vector2 position, double radius)
    {
        if (fill is null && pen is null)
        {
            return;
        }

        context.DrawEllipse(fill, pen, ToPoint(position), radius, radius);
    }

    private void DrawSquare(DrawingContext context, IBrush? fill, IBrush? outline, Vector2 position, double size)
    {
        Pen? pen = outline is null ? null : new Pen(outline, 1);
        Point point = ToPoint(position);
        context.DrawRectangle(fill, pen, new Rect(point.X - size / 2, point.Y - size / 2, size, size));
    }

    private void DrawLine(DrawingContext context, IBrush? brush, Vector2 start, Vector2 end, double thickness)
    {
        if (brush is not null)
        {
            context.DrawLine(new Pen(brush, thickness), ToPoint(start), ToPoint(end));
        }
    }

    private Point ToPoint(Vector2 point)
    {
        Vector2 transformed = transform.WorldToViewport(point);
        return new Point(transformed.X, transformed.Y);
    }
}

/// <summary>Provides the hit result attached to a visualizer interaction event.</summary>
public sealed class ObjectVisualiserHitEventArgs : EventArgs
{
    /// <summary>Creates event data for a hit, including null for a cleared selection or hover.</summary>
    /// <param name="hit">The object and sub-part that was hit.</param>
    public ObjectVisualiserHitEventArgs(ObjectVisualiserHit? hit) => Hit = hit;

    /// <summary>Gets the hit result, or <see langword="null"/> when the pointer left all objects.</summary>
    public ObjectVisualiserHit? Hit { get; }
}
