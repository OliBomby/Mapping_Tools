using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Components.Graph.Interpolation;

namespace Mapping_Tools.Components.Graph {
    public class TensionAnchor2 : GraphPointControl2 {
        protected override double DefaultSize { get; } = 10;
        
        [NotNull]
        public Anchor2 ParentAnchor { get; set; }

        private Brush _stroke;
        public override Brush Stroke {
            get => _stroke;
            set { 
                _stroke = value;
                MainShape.Stroke = value;
                if (IsDragging) {
                    MainShape.Fill = value;
                }
            }
        }

        private Brush _fill;
        public override Brush Fill {
            get => _fill;
            set {
                _fill = value;
                if (!IsDragging) {
                    MainShape.Fill = value;
                }
            }
        }

        private double _tension;
        public override double Tension {
            get => _tension; set {
                if (Math.Abs(_tension - value) < Precision.DOUBLE_EPSILON) return;
                _tension = value;
                ParentAnchor.Tension = value;
            }
        }

        public TensionAnchor2(Graph2 parent, Vector2 pos, Anchor2 parentAnchor) : base(parent, pos) {
            SetCursor();
            AbsoluteDraggingMode = true;
            ParentAnchor = parentAnchor;
        }

        private void SetCursor() {
            Cursor = IsDragging ? Cursors.None : Cursors.SizeNS;
        }

        public override void EnableDragging() {
            base.EnableDragging();
            
            MainShape.Fill = Stroke;
            SetCursor();
            Graph.UpdateVisual();
        }

        public override void DisableDragging() {
            base.DisableDragging();

            SizeMultiplier = 1;
            MainShape.Fill = Fill;
            SetCursor();
            Graph.UpdateVisual();
        }

        protected override void OnDrag(Vector2 drag, MouseEventArgs e) {
            var verticalDrag = drag.Y;

            // Ctrl on tension point makes it more precise
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                verticalDrag /= 10;
            }

            if (ParentAnchor.PreviousAnchor != null &&
                ParentAnchor.Interpolator.GetType().GetCustomAttribute<VerticalMirrorInterpolatorAttribute>() != null &&
                ParentAnchor.Pos.Y < ParentAnchor.PreviousAnchor.Pos.Y) {
                verticalDrag = -verticalDrag;
            }

            IgnoreDrag = 1;
            SetTension(Tension - verticalDrag / 200);

            IgnoreDrag = 1;
            var p = e.GetPosition(Graph);
            MoveCursorToThis(new Vector(p.X, p.Y));
            LastMousePoint = new Point(0, 0);
        }

        private void Anchor_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            ResetTension();
            Graph.UpdateVisual();

            e.Handled = true;
        }

        public override void SetTension(double tension) {
            Tension = tension;

            if (IsDragging) {
                SizeMultiplier = Math.Pow(1.5, Math.Abs(MathHelper.Clamp(Tension, -1, 1)) * 2 - 1);
            } else {
                Graph.UpdateVisual();
            }
        }
    }
}
