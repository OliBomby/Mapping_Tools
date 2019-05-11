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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Graph : UserControl {
        public List<Anchor> anchors;

        public Graph() {
            InitializeComponent();
            anchors = new List<Anchor>();
        }

        private void ThisMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                // Create an anchor right on the cursor
                Anchor anchor = new Anchor(this) {
                    Width = 10,
                    Height = 10,
                    Dragging = true
                };
                anchor.CaptureMouse();

                // Get the position of the mouse relative to the Canvas
                Point mousePos = e.GetPosition(mainCanvas);

                // Center the object on the mouse
                double left = MathHelper.Clamp(mousePos.X - anchor.Width / 2, 0, mainCanvas.ActualWidth - anchor.Width);
                double top = MathHelper.Clamp(mousePos.Y - anchor.Height / 2, 0, mainCanvas.ActualHeight - anchor.Height);
                Canvas.SetLeft(anchor, left);
                Canvas.SetTop(anchor, top);

                // Find the correct index to insert this anchor
                int index = anchors.Count;
                if (index >= 2) {
                    // Find the 2 nearest anchors
                    double distance1 = double.PositiveInfinity;
                    double distance2 = double.PositiveInfinity;
                    Anchor anchor1 = anchors[0];
                    Anchor anchor2 = anchors[1];
                    foreach (Anchor a in anchors) {
                        double dist = Distance(a.GetPosition(), mousePos);
                        if (dist < distance1) {
                            distance1 = dist;
                            anchor1 = a;
                            continue;
                        }
                        if (dist < distance2) {
                            distance2 = dist;
                            anchor2 = a;
                        }
                    }
                    index = anchors.IndexOf(anchor1);
                    if (anchors.IndexOf(anchor2) > index) {
                        index += 1;
                    }
                }

                mainCanvas.Children.Insert(index + 1, anchor);  // + 1 because there is already a PolyLine object in the canvas that has to stay at index 0
                anchors.Insert(index, anchor);
            }
            e.Handled = true;
        }

        private void ThisLayoutUpdated(object sender, EventArgs e) {
            PointCollection pointCollection = new PointCollection(anchors.Count);
            foreach (Anchor a in anchors) {
                pointCollection.Add(a.GetPosition());
            }
            line.Points = pointCollection;
        }

        private double Distance(Point p1, Point p2) {
            Vector d = p2 - p1;
            return d.Length;
        }
    }
}
