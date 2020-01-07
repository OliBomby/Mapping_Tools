using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for TensionAnchor.xaml
    /// </summary>
    public partial class TensionAnchor {
        protected override double DefaultSize { get; } = 7;
        
        public static readonly DependencyProperty ParentAnchorProperty =
            DependencyProperty.Register(nameof(ParentAnchor),
                typeof(Anchor), 
                typeof(TensionAnchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        [NotNull]
        public Anchor ParentAnchor {
            get => (Anchor) GetValue(ParentAnchorProperty);
            set => SetValue(ParentAnchorProperty, value);
        }
        
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke),
                typeof(Brush), 
                typeof(TensionAnchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnStrokeChanged));
        
        public sealed override Brush Stroke {
            get => (Brush) GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var a = (TensionAnchor) d;
            a.MainShape.Stroke = (Brush) e.NewValue;
            if (a.IsDragging) {
                a.MainShape.Fill = (Brush) e.NewValue;
            }
        }
        
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill),
                typeof(Brush), 
                typeof(TensionAnchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnFillChanged));
        
        public sealed override Brush Fill {
            get => (Brush) GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var a = (TensionAnchor) d;
            if (!a.IsDragging) {
                a.MainShape.Fill = (Brush) e.NewValue;
            }
        }
        
        public static readonly DependencyProperty TensionProperty =
            DependencyProperty.Register(nameof(Tension),
                typeof(double), 
                typeof(TensionAnchor), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnTensionChanged));
        
        public sealed override double Tension {
            get => (double) GetValue(TensionProperty);
            set => SetValue(TensionProperty, value);
        }

        private static void OnTensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var a = (TensionAnchor) d;
            a.ParentAnchor.Tension = (double) e.NewValue;
        }

        public TensionAnchor(Graph parent, Vector2 pos, Anchor parentAnchor) : base(parent, pos) {
            InitializeComponent();
            SetCursor();
            AbsoluteDraggingMode = true;
            ParentAnchor = parentAnchor;
            Stroke = parent?.TensionAnchorStroke;
            Fill = parent?.TensionAnchorFill;
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
