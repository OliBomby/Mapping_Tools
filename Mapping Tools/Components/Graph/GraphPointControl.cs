using Mapping_Tools.Classes.MathUtil;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Mapping_Tools.Components.Graph {
    public abstract class GraphPointControl : UserControl {
        protected bool IsDragging;
        protected int IgnoreDrag;

        protected bool AbsoluteDraggingMode;
        protected Point LastMousePoint;

        protected abstract double DefaultSize { get; }

        protected double Size => DefaultSize * SizeMultiplier;

        private double _sizeMultiplier;
        protected double SizeMultiplier {
            get => _sizeMultiplier;
            set {
                if (Math.Abs(_sizeMultiplier - value) < Precision.DOUBLE_EPSILON) return;
                _sizeMultiplier = value;
                SetSize(Size);
            }
        }

        public static readonly DependencyProperty GraphProperty =
            DependencyProperty.Register(nameof(Graph),
                typeof(Graph), 
                typeof(GraphPointControl), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        [JsonIgnore]
        public Graph Graph {
            get => (Graph) GetValue(GraphProperty);
            set => SetValue(GraphProperty, value);
        }

        public abstract Vector2 Pos { get; set; }

        /// <summary>
        /// Goes from -1 to 1
        /// </summary>
        public abstract double Tension { get; set; }

        public abstract Brush Stroke { get; set; }

        public abstract Brush Fill { get; set; }

        protected GraphPointControl(Graph parent) {
            SizeMultiplier = 1;
            Graph = parent; // Set graph after size multiplier to prevent an UpdateVisual call
        }

        private void SetSize(double size) {
            Width = size;
            Height = size;
            Graph?.UpdateVisual();
        }

        public virtual void EnableDragging() {
            CaptureMouse();
            IsDragging = true;
        }

        public virtual void DisableDragging() {
            ReleaseMouseCapture();
            IsDragging = false;
        }

        protected static void MoveCursorToThis(Vector relativeCursorPosition) {
            // Cursor position relative to center of this anchor
            var relativePos = FromDpi(relativeCursorPosition);
            // Cursor position on screen
            var cursorPos = System.Windows.Forms.Cursor.Position;
            // New cursor position on screen
            var newCursorPos = new System.Drawing.Point(cursorPos.X - (int)Math.Round(relativePos.X), cursorPos.Y - (int)Math.Round(relativePos.Y));
            // Set new cursor position
            System.Windows.Forms.Cursor.Position = newCursorPos;
        }

        protected static Vector2 FromDpi(Vector vector) {
            var source = PresentationSource.FromVisual(MainWindow.AppWindow);
            if (source == null) return new Vector2(vector.X, vector.Y);
            if (source.CompositionTarget == null) return new Vector2(vector.X, vector.Y);
            var dpiX = source.CompositionTarget.TransformToDevice.M11;
            var dpiY = source.CompositionTarget.TransformToDevice.M22;
            return new Vector2(vector.X * dpiX, vector.Y * dpiY);
        }

        protected Vector GetRelativeCursorPosition(MouseButtonEventArgs e) {
            return e.MouseDevice.GetPosition(this) - new Point(Width / 2, Height / 2);
        }

        protected Vector GetRelativeCursorPosition(MouseEventArgs e) {
            return e.MouseDevice.GetPosition(this) - new Point(Width / 2, Height / 2);
        }

        protected void ThisLeftMouseDown(object sender, MouseButtonEventArgs e) {
            LastMousePoint = e.GetPosition(Graph);

            // Move the cursor to the middle of this anchor
            IgnoreDrag = 2;
            MoveCursorToThis(GetRelativeCursorPosition(e));

            EnableDragging();
            e.Handled = true;
        }

        protected void ThisMouseUp(object sender, MouseButtonEventArgs e) {
            // Move the cursor to this
            IgnoreDrag = 2;
            MoveCursorToThis(GetRelativeCursorPosition(e));

            DisableDragging();
            e.Handled = true;
        }

        protected void ThisMouseMove(object sender, MouseEventArgs e) {
            if (IgnoreDrag > 0) {
                IgnoreDrag--;
                return;
            }

            if (!IsDragging) return;

            if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
                DisableDragging();
                return;
            }

            // Get the position of the mouse relative to the Canvas
            var newMousePoint = e.GetPosition(Graph);
            var diff = AbsoluteDraggingMode ? newMousePoint - LastMousePoint : GetRelativeCursorPosition(e);
            LastMousePoint = newMousePoint;

            // Handle drag
            OnDrag(new Vector2(diff.X, diff.Y), e);

            e.Handled = true;
        }

        protected abstract void OnDrag(Vector2 drag, MouseEventArgs e);

        public void ResetTension() {
            SetTension(0);
        }

        public virtual void SetTension(double tension) {
            Tension = tension;

            Graph?.UpdateVisual();
        }
    }
}