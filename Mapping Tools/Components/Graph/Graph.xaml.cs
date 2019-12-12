using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph {
        private bool _drawAnchors;
        
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State",
                typeof (GraphState),
                typeof (GraphDoubleAnimation),
                new PropertyMetadata(null));

        [NotNull]
        public GraphState State {
            get => (GraphState) GetValue(StateProperty);
            set {
                SetValue(StateProperty, value);
                UpdateVisual();
            }
        }

        public List<GraphMarker> Markers { get; set; }

        public double MinMarkerSpacing { get; set; }

        private Brush _stroke;
        public Brush Stroke {
            get => _stroke;
            set { 
                _stroke = value;
                UpdateVisual();
            }
        }

        private Brush _fill;
        public Brush Fill {
            get => _fill;
            set {
                _fill = value;
                UpdateVisual();
            }
        }

        private Brush _edgesBrush;
        public Brush EdgesBrush {
            get => _edgesBrush;
            set {
                _edgesBrush = value;
                State.Anchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _anchorStroke;
        public Brush AnchorStroke { get => _anchorStroke;
            set {
                _anchorStroke = value;
                State.Anchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _anchorFill;
        public Brush AnchorFill { get => _anchorFill;
            set {
                _anchorFill = value;
                State.Anchors.ForEach(o => o.Fill = value);
            }
        }

        private Brush _tensionAnchorStroke;
        public Brush TensionAnchorStroke { get => _tensionAnchorStroke;
            set {
                _tensionAnchorStroke = value;
                State.TensionAnchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _tensionAnchorFill;
        public Brush TensionAnchorFill { get => _tensionAnchorFill;
            set {
                _tensionAnchorFill = value;
                State.TensionAnchors.ForEach(o => o.Fill = value);
            }
        }


        public Graph() {
            InitializeComponent();
            
            Markers = new List<GraphMarker>();
            State = new GraphState(this);

            DataContext = this;

            Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            EdgesBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

            // Initialize Anchors
            State.Anchors.Add(State.MakeAnchor(new Vector2(0, 0.5)));
            State.AddAnchor(new Vector2(1, 0.5));
        }

        public void SetBrush(Brush brush) {
            var transparentBrush = brush.Clone();
            transparentBrush.Opacity = 0.2;

            Stroke = brush;
            Fill = transparentBrush;
            AnchorStroke = brush;
            AnchorFill = transparentBrush;
            TensionAnchorStroke = brush;
            TensionAnchorFill = transparentBrush;
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var newAnchor = State.AddAnchor(GetPosition(e.GetPosition(this)));
            newAnchor.EnableDragging();
        }

        private Point GetRelativePoint(Vector2 pos) {
            return new Point(pos.X * ActualWidth, ActualHeight - pos.Y * ActualHeight);
        }

        private Point GetRelativePoint(double x) {
            return GetRelativePoint(new Vector2(x, State.GetPosition(x)));
        }

        private Vector2 GetPosition(Point pos) {
            return new Vector2(pos.X / ActualWidth, (ActualHeight - pos.Y) / ActualHeight);
        }

        private Vector2 GetPosition(GraphMarker marker) {
            return GetPosition(new Point(marker.X, marker.Y));
        }

        public void UpdateVisual() {
            if (!IsInitialized) return;

            // Clear canvas
            MainCanvas.Children.Clear();

            // Add markers
            foreach (var marker in Markers.Where(marker => !(marker.X < 0) && !(marker.X > ActualWidth) && !(marker.Y < 0) && !(marker.Y > ActualHeight))) {
                MainCanvas.Children.Add(marker);
            }

            // Add border
            var rect = new Rectangle {
                Stroke = EdgesBrush, Width = ActualWidth, Height = ActualHeight, StrokeThickness = 2
            };
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            MainCanvas.Children.Add(rect);

            // Check if there are at least 2 anchors
            if (State.Anchors.Count < 2) return;

            // Calculate interpolation line
            var points = new PointCollection();
            for (int i = 1; i < State.Anchors.Count; i++) {
                var previous = State.Anchors[i - 1];
                var next = State.Anchors[i];

                points.Add(GetRelativePoint(previous.Pos));

                for (int k = 1; k < ActualWidth * (next.Pos.X - previous.Pos.X); k++) {
                    var x = previous.Pos.X + k / ActualWidth;

                    points.Add(GetRelativePoint(x));
                }
            }
            points.Add(GetRelativePoint(State.Anchors[State.Anchors.Count - 1].Pos));

            // Draw line
            var line = new Polyline {Points = points, Stroke = Stroke, StrokeThickness = 2, IsHitTestVisible = false,
                StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round};
            MainCanvas.Children.Add(line);

            // Draw area under line
            var points2 = new PointCollection(points) {
                GetRelativePoint(new Vector2(1, 0)), GetRelativePoint(new Vector2(0, 0))
            };

            var polygon = new Polygon {Points = points2, Fill = Fill, IsHitTestVisible = false};
            MainCanvas.Children.Add(polygon);

            // Return if we dont draw State.Anchors
            if (!_drawAnchors) return;

            // Add tension State.Anchors
            foreach (var tensionAnchor in State.TensionAnchors) {
                // Find x position in the middle
                var next = tensionAnchor.ParentAnchor;
                var previous = State.Anchors[State.Anchors.IndexOf(next) - 1];

                if (Math.Abs(next.Pos.X - previous.Pos.X) < Precision.DOUBLE_EPSILON) {
                    continue;
                }
                var x = (next.Pos.X + previous.Pos.X) / 2;

                // Get y on the graph and set position
                var y = State.GetPosition(x);
                tensionAnchor.Pos = new Vector2(x, y);

                RenderGraphPoint(tensionAnchor);
            }

            // Add State.Anchors
            foreach (var anchor in State.Anchors) {
                RenderGraphPoint(anchor);
            }
        }

        private void RenderGraphPoint(GraphPointControl point) {
            MainCanvas.Children.Add(point);
            var p = GetRelativePoint(point.Pos);
            Canvas.SetLeft(point, p.X - point.Width / 2);
            Canvas.SetTop(point, p.Y - point.Height / 2);
        }

        public void MoveAnchorTo(Anchor anchor, Vector2 pos) {
            var index = State.Anchors.IndexOf(anchor);
            var previous = State.Anchors.ElementAtOrDefault(index - 1);
            var next = State.Anchors.ElementAtOrDefault(index + 1);
            if (previous == null || next == null) {
                // Is edge anchor so dont move it
                pos.X = anchor.Pos.X;
            } else {
                // Snap to nearest vertical marker unless left alt is held
                if (!Keyboard.IsKeyDown(Key.LeftAlt)) {
                    // Find the nearest marker
                    GraphMarker nearestMarker = null;
                    double nearestDistance = double.PositiveInfinity;
                    foreach (var marker in Markers.Where(o => o.Orientation == Orientation.Vertical)) {
                        var markerPos = GetPosition(marker);
                        var dist = Math.Abs(pos.X - markerPos.X);
                        if (!(dist < nearestDistance)) continue;
                        nearestDistance = dist;
                        nearestMarker = marker;
                    }
                    // Set X to that marker's value
                    if (nearestMarker != null)
                        pos.X = GetPosition(nearestMarker).X;
                }
                pos.X = MathHelper.Clamp(pos.X, previous.Pos.X, next.Pos.X);
            }

            pos.Y = MathHelper.Clamp(pos.Y, 0, 1);

            anchor.Pos = pos;

            UpdateVisual();
        }

        private void Graph_OnLoaded(object sender, RoutedEventArgs e) {
            UpdateVisual();
        }

        private void Graph_OnMouseEnter(object sender, MouseEventArgs e) {
            if (_drawAnchors) return;
            _drawAnchors = true;
            UpdateVisual();
        }

        private void Graph_OnMouseLeave(object sender, MouseEventArgs e) {
            if (!_drawAnchors) return;
            _drawAnchors = false;
            UpdateVisual();
        }

        public void SetMarkers(List<GraphMarker> markers) {
            Markers = markers;
            UpdateMarkers();
        }

        private void UpdateMarkers() {
            var prevHorizontal = double.NegativeInfinity;
            var prevVertical = double.NegativeInfinity;
            foreach (var graphMarker in Markers) {
                graphMarker.Stroke = EdgesBrush;
                graphMarker.Width = ActualWidth;
                graphMarker.Height = ActualHeight;
                if (graphMarker.Orientation == Orientation.Horizontal) {
                    graphMarker.X = 0;
                    graphMarker.Y = ActualHeight - ActualHeight * ((graphMarker.Value - State.YMin) / (State.YMax - State.YMin));
                    graphMarker.Visible = Math.Abs(prevHorizontal - graphMarker.Y) >= MinMarkerSpacing;
                    if (graphMarker.Visible) {
                        prevHorizontal = graphMarker.Y;
                    }
                } else {
                    graphMarker.X = ActualWidth * ((graphMarker.Value - State.XMin) / (State.XMax - State.XMin));
                    graphMarker.Y = 0;
                    graphMarker.Visible = Math.Abs(prevVertical - graphMarker.X) >= MinMarkerSpacing;
                    if (graphMarker.Visible) {
                        prevVertical = graphMarker.X;
                    }
                }
                graphMarker.InvalidateVisual();
            }
        }

        private void Graph_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateMarkers();
            UpdateVisual();
        }
    }
}
