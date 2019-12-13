using Mapping_Tools.Classes.SliderPathStuff;
using System;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class HitObjectElement : FrameworkElement {
        private SliderPath sliderPath;
        private Geometry simpleSliderPathGeometry;

        private Geometry sliderPathGeometry;

        private Rect Bounds;

        private TranslateTransform figureTranslate;
        private double scale;
        private Transform figureTransform;

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
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateGeometryTransform();
        }

        private void UpdateGeometryTransform() {
            scale = Math.Min(ActualWidth / Bounds.Width,
                ActualHeight / Bounds.Height);
            figureTranslate = new TranslateTransform(-Bounds.Left, -Bounds.Top);
            figureTransform = new TransformGroup {
                Children = new TransformCollection {
                    figureTranslate,
                    new ScaleTransform(scale, scale, 0, 0)
                }
            };

            if (sliderPathGeometry == null) return;
            sliderPathGeometry.Transform = figureTransform;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            
            if (HitObject == null) return;

            var outlinePen = GetOutlinePen();
            
            if (HitObject.IsSlider && sliderPathGeometry != null) {
                drawingContext.DrawGeometry(Fill, outlinePen, sliderPathGeometry);

                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometryAtProgress(0));
                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometryAtProgress(1));

                if (Progress < 1 && Progress > 0) {
                    drawingContext.DrawGeometry(Fill, GetSliderBallPen(), GetCircleGeometryAtProgress(Progress));
                }
            } else if (HitObject.IsCircle) {
                Console.WriteLine("Drawing circle!");
                var geom = GetCircleGeometry(HitObject.Pos);
                Console.WriteLine(geom.Bounds);
                drawingContext.DrawGeometry(Fill, outlinePen, GetCircleGeometry(HitObject.Pos));
            }
        }

        private Pen GetOutlinePen() {
            return Stroke == null ? null : new Pen(Stroke, Thickness * scale * BorderThickness);
        }

        private Pen GetSliderBallPen() {
            return SliderBallStroke == null ? null : new Pen(SliderBallStroke, Thickness * scale * BorderThickness);
        }

        private Geometry GetProgressGeometry(double[] progresses) {
            var geom = new StreamGeometry();

            using (StreamGeometryContext gc = geom.Open()) {
                foreach (var progress in progresses) {
                    var pos = sliderPath.PositionAt(progress);
                    gc.BeginFigure(new Point(pos.X, pos.Y - 20), true, true);
                    gc.ArcTo(new Point(pos.X, pos.Y + 20), new Size(20, 20), Math.PI, true, SweepDirection.Clockwise, true, true);
                    gc.ArcTo(new Point(pos.X, pos.Y - 20), new Size(20, 20), Math.PI, true, SweepDirection.Clockwise, true, true);
                }
            }

            geom.Transform = figureTransform;
            geom.FillRule = FillRule.Nonzero;

            return geom;
        }

        private Geometry GetCircleGeometryAtProgress(double progress) {
            var pos = sliderPath.PositionAt(progress);
            return GetCircleGeometry(pos);
        }

        private Geometry GetCircleGeometry(Vector2 pos) {
            var geom = new EllipseGeometry(new Point(pos.X, pos.Y), ThicknessWithoutOutline * 0.5, ThicknessWithoutOutline * 0.5, figureTransform);
            return geom;
        }

        private static void OnHitObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var me = (HitObjectElement) d; 

            var hitObject = (Classes.BeatmapHelper.HitObject) e.NewValue;
            if (hitObject.IsSlider) {

                var geom = new StreamGeometry();
                var path = hitObject.GetSliderPath();

                var num = Math.Ceiling(path.Distance / 6);

                using (StreamGeometryContext gc = geom.Open()) {
                    gc.BeginFigure(new Point(hitObject.Pos.X, hitObject.Pos.Y), false, false);
                    for (int i = 0; i <= num; i++) {
                        var pos = path.PositionAt(i / num);
                        gc.LineTo(new Point(pos.X, pos.Y), true, true);
                    }
                }

                me.sliderPath = path;
                me.simpleSliderPathGeometry = geom;
                me.UpdateSliderPathGeometry();
            } else {
                me.Bounds = new Rect(new Point(hitObject.Pos.X - me.Thickness * 0.5, hitObject.Pos.Y - me.Thickness * 0.5),
                    new Size(me.Thickness, me.Thickness));
                me.UpdateGeometryTransform();
            }
        }

        private void UpdateSliderPathGeometry() {
            if (simpleSliderPathGeometry == null) return;

            var geom2 = simpleSliderPathGeometry.GetWidenedPathGeometry(new Pen(null, ThicknessWithoutOutline)).GetOutlinedPathGeometry();

            sliderPathGeometry = geom2;
            Bounds = simpleSliderPathGeometry.Bounds;
            Bounds.Inflate(Thickness * 0.5, Thickness * 0.5);
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