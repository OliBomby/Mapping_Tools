using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Anchor.xaml
    /// </summary>
    public partial class Anchor : UserControl {
        public bool Dragging;
        public bool MovableX { get; set; }
        public bool MovableY { get; set; }
        public bool Editable { get; set; }
        public bool ClipBounds { get; set; }

        private bool red;
        public bool Red { get => GetRed(); set => SetRed(value); }

        private bool GetRed() {
            return red;
        }

        private void SetRed(bool value) {
            red = value;
            UpdateColour();
        }

        public event ChangedHandler Changed;
        public EventArgs e = null;
        public delegate void ChangedHandler(Anchor m, EventArgs e);
        Graph ParentGraph;

        public Anchor(Graph parent) {
            InitializeComponent();
            ParentGraph = parent;
            Width = 10;
            Height = 10;
            Dragging = false;
            MovableX = true;
            MovableY = true;
            Editable = true;
            ClipBounds = false;
            UpdateColour();
        }

        public Point GetPosition() {
            return new Point(Canvas.GetLeft(this) + Width / 2, Canvas.GetTop(this) + Height / 2);
        }

        public void SetPosition(Point pos) {
            double left = pos.X;
            double top = pos.Y;

            if (ClipBounds) {
                left = MathHelper.Clamp(left, 0, ParentGraph.ActualWidth);
                top = MathHelper.Clamp(top, 0, ParentGraph.ActualHeight);
            }

            left -= Width / 2;
            top -= Height / 2;

            if (MovableX)
                Canvas.SetLeft(this, left);
            if (MovableY)
                Canvas.SetTop(this, top);

            Changed?.Invoke(this, e);
        }

        public Vector2 GetVector() {
            return new Vector2(Canvas.GetLeft(this) + Width / 2, Canvas.GetTop(this) + Height / 2);
        }

        private void UpdateColour() {
            if (Red) {
                rect.Fill = Brushes.Red;
            } else {
                rect.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#AAAAAA"));
            }
            Changed?.Invoke(this, e);
        }

        private void ThisLeftMouseDown(object sender, MouseButtonEventArgs e) {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Editable) {
                Red = true;
            }
            Dragging = true;
            CaptureMouse();
            e.Handled = true;
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (!Editable) {
                e.Handled = true;
                return;
            }

            if (Red) {
                Red = false;
                return;
            }
            ParentGraph.RemoveAnchor(this);
            Changed?.Invoke(this, e);
            e.Handled = true;
        }

        private void ThisMouseUp(object sender, MouseButtonEventArgs e) {
            var element = (UIElement)sender;
            Dragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private void ThisMouseMove(object sender, MouseEventArgs e) {
            if (Dragging && e.LeftButton == MouseButtonState.Pressed) {
                // Get the position of the mouse relative to the Canvas
                var mousePos = e.GetPosition(ParentGraph);

                // Center the object on the mouse
                SetPosition(mousePos);

                // Look for red anchors
                if (!Red && Editable) {
                    int i = ParentGraph.Anchors.IndexOf(this);
                    bool nextOverlap = i + 1 < ParentGraph.Anchors.Count - 1 && i + 1 >= 0 && ParentGraph.Anchors[i + 1].GetVector() == GetVector() && !ParentGraph.Anchors[i + 1].Red;
                    bool prevOverlap = i - 1 < ParentGraph.Anchors.Count - 1 && i - 1 >= 0 && ParentGraph.Anchors[i - 1].GetVector() == GetVector() && !ParentGraph.Anchors[i - 1].Red;
                    if (prevOverlap) {
                        Red = true;
                        ParentGraph.RemoveAnchor(i - 1);
                    } else if (nextOverlap) {
                        Red = true;
                        ParentGraph.RemoveAnchor(i + 1);
                    }
                }

                Changed?.Invoke(this, e);
            }
        }
    }
}
