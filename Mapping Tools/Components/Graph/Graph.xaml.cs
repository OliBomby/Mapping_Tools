using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
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

        public void RemoveAnchor(Anchor anchor) {
            mainCanvas.Children.Remove(anchor);
            anchors.Remove(anchor);
            AnchorsUpdated(this, null);
        }

        public void RemoveAnchor(int index) {
            RemoveAnchor(anchors[index]);
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
                anchor.Changed += AnchorsUpdated;

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
                    // Find the nearest line segment
                    double nearest = double.PositiveInfinity;
                    for (int i = 0; i < anchors.Count - 1; i++) {
                        Classes.MathUtil.LineSegment line = new Classes.MathUtil.LineSegment(anchors[i].GetVector(), anchors[i + 1].GetVector());
                        double dist = Classes.MathUtil.LineSegment.Distance(line, new Vector2(mousePos));

                        if (dist < nearest) {
                            nearest = dist;
                            index = i + 1;
                        }
                    }
                }

                mainCanvas.Children.Insert(index + 2, anchor);  // + 1 because there is already a PolyLine object in the canvas that has to stay at index 0
                anchors.Insert(index, anchor);
            }
            e.Handled = true;
        }

        private void AnchorsUpdated(object sender, EventArgs e) {
            PointCollection pointCollection = new PointCollection(anchors.Count);
            foreach (Anchor a in anchors) {
                pointCollection.Add(a.GetPosition());
            }
            line.Points = pointCollection;

            SliderPath path = new SliderPath(PathType.PerfectCurve, GetAnchorVectors().ToArray());
            List<Vector2> calculatedPath = new List<Vector2>();
            path.GetPathToProgress(calculatedPath, 0, 1);

            PointCollection pathCollection = new PointCollection();
            foreach (Vector2 v in calculatedPath) {
                pathCollection.Add(new Point(v.X, v.Y));
            }
            pathLine.Points = pathCollection;
        }

        private double Distance(Point p1, Point p2) {
            Vector d = p2 - p1;
            return d.Length;
        }

        private List<Vector2> GetAnchorVectors() {
            List<Vector2> convertedAnchors = new List<Vector2>(anchors.Count * 2);
            foreach (Anchor a in anchors) {
                convertedAnchors.Add(a.GetVector());
                if (a.Red) {  // Red anchors count double
                    convertedAnchors.Add(a.GetVector());
                }
            }
            return convertedAnchors;
        }
    }
}
