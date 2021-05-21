using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class HitObjectElement : FrameworkElement {
        private SliderPath _sliderPath;
        private Geometry _simpleSliderPathGeometry;
        private Geometry _sliderPathGeometry;
        private Rect _bounds;
        private TranslateTransform _figureTranslate;
        private double _scale;
        private Transform _figureTransform;

        public static readonly double MaxPixelLength = 1e5;

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

        private double ThicknessWithoutOutline => (1 - BorderThickness) * Thickness;

        public HitObjectElement() {
            ExtraMarkers = new ObservableCollection<HitObjectElementMarker>();
            ExtraMarkers.CollectionChanged += ExtraMarkersOnCollectionChanged;
            SizeChanged += OnSizeChanged;
        }

        private void ExtraMarkersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            InvalidateVisual();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateGeometryTransform();
        }

        private void UpdateGeometryTransform() {
            _scale = Math.Min(ActualWidth / _bounds.Width,
                ActualHeight / _bounds.Height);
            _figureTranslate = new TranslateTransform(-_bounds.Left, -_bounds.Top);
            _figureTransform = new TransformGroup {
                Children = new TransformCollection {
                    _figureTranslate,
                    new ScaleTransform(_scale, _scale, 0, 0)
                }
            };

            if (_sliderPathGeometry == null) return;
            _sliderPathGeometry.Transform = _figureTransform;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            
            if (HitObject == null) return;

            var outlinePen = GetOutlinePen();
            
            if (HitObject.IsSlider && _sliderPathGeometry != null) {
                drawingContext.DrawGeometry(Fill, outlinePen, _sliderPathGeometry);

                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometryAtProgress(0));
                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometryAtProgress(1));

                if (Progress <= 1 && Progress >= 0) {
                    drawingContext.DrawGeometry(Fill, GetSliderBallPen(), GetCircleGeometryAtProgress(Progress));
                }

                // Draw extra markers
                foreach (var marker in ExtraMarkers.Where(o => o.Progress >= 0 && o.Progress <= 1)) {
                    drawingContext.DrawGeometry(marker.Brush, new Pen(Brushes.Black, 1), 
                        GetSquareGeometryAtProgress(marker.Progress, marker.Size));
                }
            } else if (HitObject.IsCircle) {
                var geom = GetCircleGeometry(HitObject.Pos);
                Console.WriteLine(geom.Bounds);
                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometry(HitObject.Pos));
            }
        }

        private Pen GetOutlinePen() {
            return Stroke == null ? null : new Pen(Stroke, Thickness * _scale * BorderThickness);
        }

        private Pen GetSliderBallPen() {
            return SliderBallStroke == null ? null : new Pen(SliderBallStroke, Thickness * _scale * BorderThickness);
        }

        private Geometry GetProgressGeometry(double[] progresses) {
            var geom = new StreamGeometry();

            using (StreamGeometryContext gc = geom.Open()) {
                foreach (var progress in progresses) {
                    var pos = _sliderPath.PositionAt(progress);
                    gc.BeginFigure(new Point(pos.X, pos.Y - 20), true, true);
                    gc.ArcTo(new Point(pos.X, pos.Y + 20), new Size(20, 20), Math.PI, true, SweepDirection.Clockwise, true, true);
                    gc.ArcTo(new Point(pos.X, pos.Y - 20), new Size(20, 20), Math.PI, true, SweepDirection.Clockwise, true, true);
                }
            }

            geom.Transform = _figureTransform;
            geom.FillRule = FillRule.Nonzero;

            return geom;
        }

        private Geometry GetCircleGeometryAtProgress(double progress, double size = 1) {
            var pos = _sliderPath.PositionAt(progress);
            return GetCircleGeometry(pos, size);
        }

        private Geometry GetCircleGeometry(Vector2 pos, double size = 1) {
            var geom = new EllipseGeometry(new Point(pos.X, pos.Y), ThicknessWithoutOutline * 0.5 * size, ThicknessWithoutOutline * 0.5 * size, _figureTransform);
            return geom;
        }

        private Geometry GetSquareGeometryAtProgress(double progress, double size = 1) {
            var pos = _sliderPath.PositionAt(progress);
            return GetSquareGeometry(pos, size);
        }

        private Geometry GetSquareGeometry(Vector2 pos, double size = 1) {
            var geom = new RectangleGeometry(new Rect(
                new Point(pos.X - ThicknessWithoutOutline * 0.5 * size, pos.Y - ThicknessWithoutOutline * 0.5 * size),
                new Point(pos.X + ThicknessWithoutOutline * 0.5 * size, pos.Y + ThicknessWithoutOutline * 0.5 * size)), 
                0, 0, _figureTransform);
            return geom;
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

            if (hitObject.IsSlider && hitObject.PixelLength < MaxPixelLength) {
                var geom = new StreamGeometry();
                var path = CustomPixelLength == null ? hitObject.GetSliderPath() :
                    new SliderPath(hitObject.SliderType, hitObject.GetAllCurvePoints().ToArray(), CustomPixelLength);

                var num = Math.Ceiling(path.Distance / 6);

                using (StreamGeometryContext gc = geom.Open()) {
                    gc.BeginFigure(new Point(hitObject.Pos.X, hitObject.Pos.Y), false, false);
                    for (int i = 0; i <= num; i++) {
                        var pos = path.PositionAt(i / num);
                        gc.LineTo(new Point(pos.X, pos.Y), true, true);
                    }
                }

                _sliderPath = path;
                _simpleSliderPathGeometry = geom;
                UpdateSliderPathGeometry();
            } else {
                _bounds = new Rect(new Point(hitObject.Pos.X - Thickness * 0.5, hitObject.Pos.Y - Thickness * 0.5),
                    new Size(Thickness, Thickness));
                UpdateGeometryTransform();
            }
        }

        private void UpdateSliderPathGeometry() {
            if (_simpleSliderPathGeometry == null) return;

            var geom2 = _simpleSliderPathGeometry.GetWidenedPathGeometry(new Pen(null, ThicknessWithoutOutline)).GetOutlinedPathGeometry();

            _sliderPathGeometry = geom2;
            _bounds = _simpleSliderPathGeometry.Bounds;
            _bounds.Inflate(Thickness * 0.5, Thickness * 0.5);
            UpdateGeometryTransform();
        }

        private static void OnThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var me = (HitObjectElement) d; 
            if (me.HitObject != null && me.HitObject.IsSlider) {
                me.UpdateSliderPathGeometry();
            }
        }

        private static void OnBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var me = (HitObjectElement) d; 
            if (me.HitObject != null && me.HitObject.IsSlider) {
                me.UpdateSliderPathGeometry();
            }
        }
    }
}