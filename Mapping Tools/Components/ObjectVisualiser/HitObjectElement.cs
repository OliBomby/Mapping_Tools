using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class HitObjectElement : Shape {
        public Geometry SliderPathGeometry { get; set; }

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

        /*protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            if (SliderPathGeometry == null) return;
            drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.Red), 40), SliderPathGeometry);
            drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.Black), 35), SliderPathGeometry);
        }*/

        protected override Geometry DefiningGeometry {
            get {
                if (!HitObject.IsSlider) return null;

                var geom = new StreamGeometry();
                var path = HitObject.GetSliderPath();
                var num = Math.Ceiling(path.Distance / 6);

                using (StreamGeometryContext gc = geom.Open())
                {
                    gc.BeginFigure(new Point(HitObject.Pos.X, HitObject.Pos.Y), false, false);
                    for (int i = 1; i <= num; i++) {
                        var pos = path.PositionAt(i / num);
                        gc.LineTo(new Point(pos.X, pos.Y), true, true);
                    }
                }

                return geom;
            }
        }

        private static void OnHitObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var me = (HitObjectElement) d; 

            var hitObject = (Classes.BeatmapHelper.HitObject) e.NewValue;
            if (!hitObject.IsSlider) return;

            var geom = new StreamGeometry();
            me.SliderPathGeometry = geom;
            var path = hitObject.GetSliderPath();
            var num = Math.Ceiling(path.Distance / 6);

            using (StreamGeometryContext gc = geom.Open())
            {
                gc.BeginFigure(new Point(hitObject.Pos.X, hitObject.Pos.Y), false, false);
                for (int i = 1; i <= num; i++) {
                    var pos = path.PositionAt(i / num);
                    gc.LineTo(new Point(pos.X, pos.Y), true, true);
                }
            }
        }
    }
}