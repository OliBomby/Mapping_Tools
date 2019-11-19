﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph {
    public abstract class GraphPointControl : UserControl {
        protected bool IsDragging;
        protected bool IgnoreDrag;

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

        /// <summary>
        /// Ranges from (0,0) bottom left to (1,1) top right
        /// </summary>
        public Vector2 Pos { get; set; }

        public Graph Graph { get; set; }

        /// <summary>
        /// Goes from -1 to 1
        /// </summary>
        public virtual double Tension { get; set; }

        public abstract Brush Stroke { get; set; }

        public abstract Brush Fill { get; set; }

        protected GraphPointControl(Graph parent, Vector2 pos) {
            Graph = parent;
            Pos = pos;
            SizeMultiplier = 1;
        }

        private void SetSize(double size) {
            Width = size;
            Height = size;
            Graph.UpdateVisual();
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
            // Move the cursor to the middle of this anchor
            MoveCursorToThis(GetRelativeCursorPosition(e));

            EnableDragging();
            e.Handled = true;
        }

        protected void ThisMouseUp(object sender, MouseButtonEventArgs e) {
            DisableDragging();
            e.Handled = true;
        }

        protected void ThisMouseMove(object sender, MouseEventArgs e) {
            if (IgnoreDrag) {
                IgnoreDrag = false;
                return;
            }

            if (!IsDragging) return;

            if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
                DisableDragging();
                return;
            }

            // Get the position of the mouse relative to the Canvas
            var diff = GetRelativeCursorPosition(e);

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

            Graph.UpdateVisual();
        }
    }
}