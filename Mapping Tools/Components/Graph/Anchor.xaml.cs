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
            UpdateColour();
        }

        public Point GetPosition() {
            return new Point(Canvas.GetLeft(this) + Width / 2, Canvas.GetTop(this) + Height / 2);
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
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                Red = true;
            }
            Dragging = true;
            CaptureMouse();
            e.Handled = true;
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
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
                double left = MathHelper.Clamp(mousePos.X - Width / 2, 0, ParentGraph.ActualWidth - Width);
                double top = MathHelper.Clamp(mousePos.Y - Height / 2, 0, ParentGraph.ActualHeight - Height);
                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);

                // Look for red anchors
                if (!Red) {
                    int i = ParentGraph.anchors.IndexOf(this);
                    bool nextOverlap = i + 1 < ParentGraph.anchors.Count - 1 && i + 1 >= 0 && ParentGraph.anchors[i + 1].GetVector() == GetVector() && !ParentGraph.anchors[i + 1].Red;
                    bool prevOverlap = i - 1 < ParentGraph.anchors.Count - 1 && i - 1 >= 0 && ParentGraph.anchors[i - 1].GetVector() == GetVector() && !ParentGraph.anchors[i - 1].Red;
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
