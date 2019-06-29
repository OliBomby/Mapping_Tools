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
        public List<Anchor> Anchors;
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }


        public Graph() {
            InitializeComponent();
            Anchors = new List<Anchor>();
            XMin = 0;
            YMin = 0;
            XMax = 1;
            YMax = 1;
        }

        public void RemoveAnchor(Anchor anchor) {
            mainCanvas.Children.Remove(anchor);
            Anchors.Remove(anchor);
            AnchorsUpdated(this, null);
        }

        public void RemoveAnchor(int index) {
            RemoveAnchor(Anchors[index]);
        }

        private void ThisMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                // Create an anchor right on the cursor
                Anchor anchor = new Anchor(this);

                // Get the position of the mouse relative to the Canvas
                Point mousePos = e.GetPosition(mainCanvas);

                // Center the object on the mouse
                anchor.SetPosition(mousePos);

                // Find the correct index to insert this anchor
                int index = Anchors.Count;
                if (index >= 2) {
                    // Find the nearest line segment
                    double nearest = double.PositiveInfinity;
                    for (int i = 0; i < Anchors.Count - 1; i++) {
                        Classes.MathUtil.LineSegment line = new Classes.MathUtil.LineSegment(Anchors[i].GetVector(), Anchors[i + 1].GetVector());
                        double dist = Classes.MathUtil.LineSegment.Distance(line, new Vector2(mousePos));

                        if (dist < nearest) {
                            nearest = dist;
                            index = i + 1;
                        }
                    }
                }

                AddAnchor(anchor, index);
            }
            e.Handled = true;
        }

        private void AnchorsUpdated(object sender=null, EventArgs e=null) {
            PointCollection pointCollection = new PointCollection(Anchors.Count);
            foreach (Anchor a in Anchors) {
                pointCollection.Add(a.GetPosition());
            }
            line.Points = pointCollection;

            SliderPath path = new SliderPath(PathType.Bezier, GetAnchorVectorsCanvas().ToArray());
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

        private List<Vector2> GetAnchorVectorsCanvas() {
            List<Vector2> convertedAnchors = new List<Vector2>(Anchors.Count * 2);
            foreach (Anchor a in Anchors) {
                convertedAnchors.Add(a.GetVector());
                if (a.Red) {  // Red anchors count double
                    convertedAnchors.Add(a.GetVector());
                }
            }
            return convertedAnchors;
        }

        public List<Vector2> GetAnchorVectors() {
            List<Vector2> convertedAnchors = new List<Vector2>(Anchors.Count * 2);
            foreach (Anchor a in Anchors) {
                var v = a.GetVector();
                v.X = v.X / mainCanvas.ActualWidth * XMax + XMin;
                v.Y = (1 - v.Y / mainCanvas.ActualHeight) * YMax + XMin;

                convertedAnchors.Add(v);
                if (a.Red) {  // Red anchors count double
                    convertedAnchors.Add(v);
                }
            }
            return convertedAnchors;
        }

        public List<Vector2> GetGraph() {
            SliderPath path = new SliderPath(PathType.Bezier, GetAnchorVectors().ToArray());
            List<Vector2> calculatedPath = new List<Vector2>();
            path.GetPathToProgress(calculatedPath, 0, 1);
            return calculatedPath;
        }

        public void AddAnchor(Anchor anchor, int? index=null) {
            int insertIndex = index ?? Anchors.Count;

            anchor.Changed += AnchorsUpdated;
            mainCanvas.Children.Insert(insertIndex + 2, anchor);  // + 1 because there is already a PolyLine object in the canvas that has to stay at index 0
            Anchors.Insert(insertIndex, anchor);
            AnchorsUpdated();
        }
    }
}
