using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for TensionAnchor.xaml
    /// </summary>
    public partial class TensionAnchor {
        protected override double DefaultSize { get; } = 6;
        
        [NotNull]
        public Anchor ParentAnchor { get; set; }

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

        public TensionAnchor(Graph parent, Vector2 pos, Anchor parentAnchor) : base(parent, pos) {
            InitializeComponent();
            SetCursor();
            ParentAnchor = parentAnchor;
        }

        private void SetCursor() {
            Cursor = IsDragging ? Cursors.None : Cursors.SizeNS;
            Cursor = Cursors.SizeNS;
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
            // Ctrl on tension point makes it more precise
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                drag.Y /= 10;
            }

            if (ParentAnchor.PreviousAnchor != null &&
                ParentAnchor.Interpolator is NaturalExponentialInterpolator &&
                ParentAnchor.Pos.Y < ParentAnchor.PreviousAnchor.Pos.Y) {
                drag.Y = -drag.Y;
            }
            Console.WriteLine("before: " + GetRelativeCursorPosition(e));
            IgnoreDrag = 1;
            SetTension(Tension - drag.Y / 100);
            Console.WriteLine("after: " + GetRelativeCursorPosition(e));

            // Move the cursor to this
            IgnoreDrag = 1;
            MoveCursorToThis(GetRelativeCursorPosition(e));
            Console.WriteLine("after move: " + GetRelativeCursorPosition(e));
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
