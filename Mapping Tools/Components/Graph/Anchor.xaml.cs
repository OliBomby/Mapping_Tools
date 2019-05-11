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
        Graph ParentGraph;
        bool RedAnchor;

        public Anchor(Graph parent) {
            InitializeComponent();
            ParentGraph = parent;
            UpdateColour();
        }

        public Point GetPosition() {
            return new Point(Canvas.GetLeft(this) + Width / 2, Canvas.GetTop(this) + Height / 2);
        }

        private void UpdateColour() {
            if (RedAnchor) {
                rect.Fill = Brushes.Red;
            } else {
                rect.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#AAAAAA"));
            }
        }

        private void ThisLeftMouseDown(object sender, MouseButtonEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                RedAnchor = true;
                UpdateColour();
            }
            Dragging = true;
            CaptureMouse();
            e.Handled = true;
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (RedAnchor) {
                RedAnchor = false;
                UpdateColour();
                return;
            }
            ParentGraph.mainCanvas.Children.Remove(this);
            ParentGraph.anchors.Remove(this);
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
            }
        }
    }
}
