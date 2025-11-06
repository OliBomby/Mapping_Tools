using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;

namespace Mapping_Tools.Components.ObjectVisualiser;

public class HitObjectElement : FrameworkElement {
    private SliderPath sliderPath;
    private List<Vector2> controlPoints;
    private Geometry sliderPathGeometry;
    private Rect bounds;
    private TranslateTransform figureTranslate;
    private double scale;
    private Transform figureTransform;

    public const double MaxPixelLength = 1e6;
    public const double MaxSegmentCount = 1e6;
    public const double MaxAnchorCount = 1500;
    public const int HardMaxAnchorCount = 5000;

    #region Properties

    public static readonly DependencyProperty HitObjectProperty =
        DependencyProperty.Register("HitObject",
            typeof(Classes.BeatmapHelper.HitObject), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnHitObjectChanged
            ));

    public Classes.BeatmapHelper.HitObject HitObject {
        get => (Classes.BeatmapHelper.HitObject) GetValue(HitObjectProperty);
        set => SetValue(HitObjectProperty, value);
    }

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register("Progress",
            typeof(double), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(-1d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress {
        get => (double) GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public static readonly DependencyProperty CustomPixelLengthProperty =
        DependencyProperty.Register("CustomPixelLength",
            typeof(double?), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                OnCustomPixelLengthChanged));

    public double? CustomPixelLength {
        get => (double?) GetValue(CustomPixelLengthProperty);
        set => SetValue(CustomPixelLengthProperty, value);
    }

    public static readonly DependencyProperty ShowAnchorsProperty =
        DependencyProperty.Register("ShowAnchors",
            typeof(bool),
            typeof(HitObjectElement),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool ShowAnchors {
        get => (bool) GetValue(ShowAnchorsProperty);
        set => SetValue(ShowAnchorsProperty, value);
    }

    public static readonly DependencyProperty ExtraMarkersProperty =
        DependencyProperty.Register(nameof(ExtraMarkers),
            typeof(ObservableCollection<HitObjectElementMarker>), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                OnExtraMarkersChanged));

    public ObservableCollection<HitObjectElementMarker> ExtraMarkers {
        get => (ObservableCollection<HitObjectElementMarker>) GetValue(ExtraMarkersProperty);
        set => SetValue(ExtraMarkersProperty, value);
    }

    public static readonly DependencyProperty ThicknessProperty =
        DependencyProperty.Register("Thickness",
            typeof(double),
            typeof(HitObjectElement),
            new FrameworkPropertyMetadata(40d, FrameworkPropertyMetadataOptions.AffectsRender,
                OnThicknessChanged));

    public double Thickness {
        get => (double) GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register("BorderThickness",
            typeof(double), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(0.1d, FrameworkPropertyMetadataOptions.AffectsRender,
                OnBorderThicknessChanged));

    public double BorderThickness {
        get => (double) GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public static readonly DependencyProperty AnchorSizeProperty =
        DependencyProperty.Register("AnchorSize",
            typeof(double),
            typeof(HitObjectElement),
            new FrameworkPropertyMetadata(0.2d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double AnchorSize {
        get => (double) GetValue(AnchorSizeProperty);
        set => SetValue(AnchorSizeProperty, value);
    }

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register("Stroke",
            typeof(Brush), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Stroke {
        get => (Brush) GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register("Fill",
            typeof(Brush), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Fill {
        get => (Brush) GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty SliderBallStrokeProperty =
        DependencyProperty.Register("SliderBallStroke",
            typeof(Brush), 
            typeof(HitObjectElement), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush SliderBallStroke {
        get => (Brush) GetValue(SliderBallStrokeProperty);
        set => SetValue(SliderBallStrokeProperty, value);
    }

    #endregion

    private double ThicknessWithoutOutline => (1 - BorderThickness) * Thickness;

    private double ThicknessInsideOutline => (1 - BorderThickness * 2) * Thickness;

    public HitObjectElement() {
        ExtraMarkers = new ObservableCollection<HitObjectElementMarker>();
        ExtraMarkers.CollectionChanged += ExtraMarkersOnCollectionChanged;
        SizeChanged += OnSizeChanged;
    }

    private void ExtraMarkersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        InvalidateVisual();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        UpdateTransform();
    }

    private void UpdateBounds() {
        if (sliderPathGeometry is null) return;
        bounds = sliderPathGeometry.Bounds;
        bounds.Inflate(Thickness * 0.5, Thickness * 0.5);
        UpdateTransform();
    }

    private void UpdateTransform() {
        scale = Math.Min(ActualWidth / bounds.Width,
            ActualHeight / bounds.Height);
        figureTranslate = new TranslateTransform(-bounds.Left, -bounds.Top);
        figureTransform = new TransformGroup {
            Children = new TransformCollection {
                figureTranslate,
                new ScaleTransform(scale, scale, 0, 0)
            }
        };
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext) {
        base.OnRender(drawingContext);
            
        if (HitObject == null) return;

        drawingContext.PushTransform(figureTransform);

        var outlinePen = GetOutlinePen();
        var blackOutlinePen = GetBlackFinePen();
            
        if (HitObject.IsSlider && sliderPathGeometry != null) {
            drawingContext.DrawGeometry(null, GetPathOutlinePen(), sliderPathGeometry);
            drawingContext.DrawGeometry(null, GetPathFillPen(), sliderPathGeometry);

            DrawCircleAtProgress(drawingContext, Fill, outlinePen, 0);
            DrawCircleAtProgress(drawingContext, Fill, outlinePen, 1);

            if (Progress is <= 1 and >= 0) {
                DrawCircleAtProgress(drawingContext, Fill, GetSliderBallPen(), Progress);
            }

            if (ShowAnchors && controlPoints.Count <= MaxAnchorCount) {
                // Draw the lines between the anchors
                var anchors = controlPoints;
                var controlPointLinePen = GetWhiteFinePen();
                for (var i = 0; i < anchors.Count - 1; i++) {
                    DrawLine(drawingContext, controlPointLinePen, anchors[i], anchors[i + 1]);
                }
                // Draw the slider anchors
                for (var i = 0; i < anchors.Count; i++) {
                    var fill = i != 0 && anchors[i] == anchors[i - 1] ? Brushes.Red : Brushes.LightGray;
                    DrawSquare(drawingContext, fill, blackOutlinePen, anchors[i], AnchorSize);
                }
            }

            // Draw extra markers
            foreach (var marker in ExtraMarkers.Where(o => o.Progress is >= 0 and <= 1)) {
                DrawSquareAtProgress(drawingContext, marker.Brush, blackOutlinePen, marker.Progress, marker.Size);
            }
        } else if (HitObject.IsCircle) {
            DrawCircle(drawingContext, Fill, outlinePen, HitObject.Pos);
        }
    }

    private Pen GetPathFillPen() {
        return Fill == null ? null : new Pen(Fill, ThicknessInsideOutline);
    }

    private Pen GetPathOutlinePen() {
        return Fill == null ? null : new Pen(Stroke, Thickness);
    }

    private Pen GetOutlinePen() {
        return Stroke == null ? null : new Pen(Stroke, Thickness * BorderThickness);
    }

    private Pen GetSliderBallPen() {
        return SliderBallStroke == null ? null : new Pen(SliderBallStroke, Thickness * BorderThickness);
    }

    private Pen GetBlackFinePen() {
        return new Pen(Brushes.Black, 1);
    }

    private Pen GetWhiteFinePen() {
        return new Pen(Brushes.White, 1);
    }

    private void DrawCircleAtProgress(DrawingContext ctx, Brush brush, Pen pen, double progress, double size = 1) {
        var pos = sliderPath.PositionAt(progress);
        DrawCircle(ctx, brush, pen, pos, size);
    }

    private void DrawCircle(DrawingContext ctx, Brush brush, Pen pen, Vector2 pos, double size = 1) {
        ctx.DrawEllipse(brush, pen, new Point(pos.X, pos.Y), ThicknessWithoutOutline * 0.5 * size, ThicknessWithoutOutline * 0.5 * size);
    }

    private void DrawSquareAtProgress(DrawingContext ctx, Brush brush, Pen pen, double progress, double size = 1) {
        var pos = sliderPath.PositionAt(progress);
        DrawSquare(ctx, brush, pen, pos, size);
    }

    private void DrawSquare(DrawingContext ctx, Brush brush, Pen pen, Vector2 pos, double size = 1) {
        ctx.DrawRectangle(brush, pen, new Rect(
            new Point(pos.X - 0.5 * size, pos.Y - 0.5 * size),
            new Point(pos.X + 0.5 * size, pos.Y + 0.5 * size)));
    }

    private static void DrawLine(DrawingContext ctx, Pen pen, Vector2 p1, Vector2 p2) {
        ctx.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
    }

    private static void OnHitObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (HitObjectElement) d; 

        var hitObject = (Classes.BeatmapHelper.HitObject) e.NewValue;
        me.SetHitObject(hitObject);
    }

    private static void OnCustomPixelLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (HitObjectElement) d; 

        me.SetHitObject(me.HitObject);
    }

    private static void OnExtraMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (HitObjectElement) d; 

        me.ExtraMarkers.CollectionChanged += me.ExtraMarkersOnCollectionChanged;
    }

    private void SetHitObject(Classes.BeatmapHelper.HitObject hitObject) {
        if (hitObject == null) return;

        if (hitObject.IsSlider && hitObject.PixelLength < MaxPixelLength && hitObject.CurvePoints.Count < HardMaxAnchorCount) {
            var geom = new StreamGeometry();
            var path = CustomPixelLength == null ? hitObject.GetSliderPath() :
                new SliderPath(hitObject.SliderType, hitObject.GetAllCurvePoints().ToArray(), CustomPixelLength);

            if (path.CalculatedPath.Count > MaxSegmentCount) return;

            using (StreamGeometryContext gc = geom.Open()) {
                gc.BeginFigure(new Point(hitObject.Pos.X, hitObject.Pos.Y), false, false);
                foreach (Vector2 pos in path.CalculatedPath) {
                    gc.LineTo(new Point(pos.X, pos.Y), true, true);
                }
            }

            sliderPath = path;
            controlPoints = path.ControlPoints;
            sliderPathGeometry = geom;
            UpdateBounds();
        } else {
            var point = new Point(hitObject.Pos.X, hitObject.Pos.Y);
            sliderPathGeometry = new LineGeometry(point, point);
            UpdateBounds();
        }
    }

    private static void OnThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (HitObjectElement) d; 
        if (me.HitObject is { IsSlider: true }) {
            me.UpdateBounds();
        }
    }

    private static void OnBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (HitObjectElement) d; 
        if (me.HitObject is { IsSlider: true }) {
            me.UpdateTransform();
        }
    }
}