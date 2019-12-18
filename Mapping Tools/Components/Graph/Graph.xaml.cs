using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using Mapping_Tools.Components.Graph.Markers;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph {
        private bool _drawAnchors;
        private readonly List<GraphMarker> _markers;

        public event DependencyPropertyChangedEventHandler GraphStateChanged;

        #region DependencyProperties

        public static readonly DependencyProperty TensionAnchorsProperty =
            DependencyProperty.Register(nameof(TensionAnchors),
                typeof(ObservableCollection<TensionAnchor>), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null));

        public ObservableCollection<TensionAnchor> TensionAnchors {
            get => (ObservableCollection<TensionAnchor>) GetValue(TensionAnchorsProperty);
            set => SetValue(TensionAnchorsProperty, value);
        }

        public static readonly DependencyProperty AnchorsProperty =
            DependencyProperty.Register(nameof(Anchors),
                typeof(ObservableCollection<Anchor>), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnAnchorsChanged));

        public ObservableCollection<Anchor> Anchors {
            get => (ObservableCollection<Anchor>) GetValue(AnchorsProperty);
            set => SetValue(AnchorsProperty, value);
        }

        public static readonly DependencyProperty LastInterpolationSetProperty =
            DependencyProperty.Register(nameof(LastInterpolationSet),
                typeof(Type), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null));

        [NotNull]
        public Type LastInterpolationSet {
            get => (Type) GetValue(LastInterpolationSetProperty);
            set => SetValue(LastInterpolationSetProperty, value);
        }

        public static readonly DependencyProperty MinXProperty =
            DependencyProperty.Register(nameof(MinX),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnBoundsChanged));

        public double MinX {
            get => (double) GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }

        public static readonly DependencyProperty MinYProperty =
            DependencyProperty.Register(nameof(MinY),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnBoundsChanged));

        public double MinY {
            get => (double) GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }

        public static readonly DependencyProperty MaxXProperty =
            DependencyProperty.Register(nameof(MaxX),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.None,
                    OnBoundsChanged));

        public double MaxX {
            get => (double) GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }

        public static readonly DependencyProperty MaxYProperty =
            DependencyProperty.Register(nameof(MaxY),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.None,
                    OnBoundsChanged));

        public double MaxY {
            get => (double) GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }

        public static readonly DependencyProperty HorizontalMarkerGeneratorProperty =
            DependencyProperty.Register(nameof(HorizontalMarkerGenerator),
                typeof(IMarkerGenerator), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnMarkerGeneratorsChanged));

        public IMarkerGenerator HorizontalMarkerGenerator {
            get => (IMarkerGenerator) GetValue(HorizontalMarkerGeneratorProperty);
            set => SetValue(HorizontalMarkerGeneratorProperty, value);
        }

        public static readonly DependencyProperty VerticalMarkerGeneratorProperty =
            DependencyProperty.Register(nameof(VerticalMarkerGenerator),
                typeof(IMarkerGenerator), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnMarkerGeneratorsChanged));

        public IMarkerGenerator VerticalMarkerGenerator {
            get => (IMarkerGenerator) GetValue(VerticalMarkerGeneratorProperty);
            set => SetValue(VerticalMarkerGeneratorProperty, value);
        }

        public static readonly DependencyProperty MinMarkerSpacingProperty =
            DependencyProperty.Register(nameof(MinMarkerSpacing),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.None,
                    OnMarkersChanged));

        public double MinMarkerSpacing {
            get => (double) GetValue(MinMarkerSpacingProperty);
            set => SetValue(MinMarkerSpacingProperty, value);
        }

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnVisualChanged));

        public Brush Stroke {
            get => (Brush) GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnVisualChanged));

        public Brush Fill {
            get => (Brush) GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty EdgesBrushProperty =
            DependencyProperty.Register(nameof(EdgesBrush),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnEdgesBrushChanged));

        public Brush EdgesBrush {
            get => (Brush) GetValue(EdgesBrushProperty);
            set => SetValue(EdgesBrushProperty, value);
        }

        public static readonly DependencyProperty AnchorStrokeProperty =
            DependencyProperty.Register(nameof(AnchorStroke),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnAnchorStrokeChanged));

        public Brush AnchorStroke {
            get => (Brush) GetValue(AnchorStrokeProperty);
            set => SetValue(AnchorStrokeProperty, value);
        }

        public static readonly DependencyProperty AnchorFillProperty =
            DependencyProperty.Register(nameof(AnchorFill),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnAnchorFillChanged));

        public Brush AnchorFill {
            get => (Brush) GetValue(AnchorFillProperty);
            set => SetValue(AnchorFillProperty, value);
        }

        public static readonly DependencyProperty TensionAnchorStrokeProperty =
            DependencyProperty.Register(nameof(TensionAnchorStroke),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnTensionAnchorStrokeChanged));

        public Brush TensionAnchorStroke {
            get => (Brush) GetValue(TensionAnchorStrokeProperty);
            set => SetValue(TensionAnchorStrokeProperty, value);
        }

        public static readonly DependencyProperty TensionAnchorFillProperty =
            DependencyProperty.Register(nameof(TensionAnchorFill),
                typeof(Brush), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnTensionAnchorFillChanged));

        public Brush TensionAnchorFill {
            get => (Brush) GetValue(TensionAnchorFillProperty);
            set => SetValue(TensionAnchorFillProperty, value);
        }

        #endregion

        public Graph() {
            InitializeComponent();
            
            _markers = new List<GraphMarker>();
            TensionAnchors = new ObservableCollection<TensionAnchor>();
            Anchors = new ObservableCollection<Anchor>();
            LastInterpolationSet = typeof(SingleCurveInterpolator);

            Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            EdgesBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

            // Initialize Anchors
            Anchors.Add(MakeAnchor(new Vector2(0, 0.5)));
            AddAnchor(new Vector2(1, 0.5));

            Anchors.CollectionChanged += AnchorsOnCollectionChanged;
        }

        private void AnchorsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            GraphStateChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(AnchorsProperty, Anchors, Anchors));
        }

        /// <summary>
        /// Gets the freezable state of the graph.
        /// </summary>
        /// <returns></returns>
        public GraphState GetGraphState() {
            return new GraphState {
                Anchors = Anchors.ToList(), TensionAnchors = TensionAnchors.ToList(),
                MinX = MinX, MinY = MinY, MaxX = MaxX, MaxY = MaxY
            };
        }

        #region GraphStuff

        public Anchor AddAnchor(Vector2 pos) {
            // Clamp the position withing bounds
            pos = Vector2.Clamp(pos, Vector2.Zero, Vector2.One);

            // Find the correct index
            var rightAnchor = Anchors.FirstOrDefault(o => o.Pos.X >= pos.X);
            var index = rightAnchor == null ? Math.Max(Anchors.Count - 1, 1) : Anchors.IndexOf(rightAnchor);

            // Get the next anchor
            Anchor nextAnchor = null;
            if (index < Anchors.Count) {
                nextAnchor = Anchors[index];
            }

            // Make anchor
            var anchor = MakeAnchor(pos, LastInterpolationSet);

            // Make tension anchor
            var tensionAnchor = MakeTensionAnchor(pos, anchor);

            // Link State.Anchors
            anchor.TensionAnchor = tensionAnchor;

            // Add tension
            anchor.Tension = nextAnchor?.Tension ?? 0;
            
            // Insert anchor
            Anchors.Insert(index, anchor);

            // Add tension anchor
            TensionAnchors.Add(tensionAnchor);
            
            UpdateAnchorNeighbors();
            UpdateVisual();

            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos) {
            var anchor = new Anchor(this, pos) {
                Stroke = AnchorStroke,
                Fill = AnchorFill
            };
            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos, Type interpolator) {
            var anchor = new Anchor(this, pos, interpolator) {
                Stroke = AnchorStroke,
                Fill = AnchorFill
            };
            return anchor;
        }

        public TensionAnchor MakeTensionAnchor(Vector2 pos, Anchor parentAnchor) {
            var anchor = new TensionAnchor(this, pos, parentAnchor) {
                Stroke = AnchorStroke,
                Fill = AnchorFill
            };
            return anchor;
        }

        public void RemoveAnchor(Anchor anchor) {
            // Dont remove the anchors on the left and right edge
            if (IsEdgeAnchor(anchor)) return;

            Anchors.Remove(anchor);
            TensionAnchors.Remove(anchor.TensionAnchor);

            UpdateAnchorNeighbors();
            UpdateVisual();
        }

        private void UpdateAnchorNeighbors() {
            Anchor previousAnchor = null;
            foreach (var anchor in Anchors) {
                anchor.PreviousAnchor = previousAnchor;
                if (previousAnchor != null) {
                    previousAnchor.NextAnchor = anchor;
                }

                previousAnchor = anchor;
            }
        }

        public bool IsEdgeAnchor(Anchor anchor) {
            return anchor == Anchors[0] || anchor == Anchors[Anchors.Count - 1];
        }

        /// <summary>
        /// Calculates the height of the curve [0-1] for a given progression allong the graph [0-1].
        /// </summary>
        /// <param name="x">The progression along the curve (0-1)</param>
        /// <returns>The height of the curve (0-1)</returns>
        public double GetPosition(double x) {
            // Find the section
            var previousAnchor = Anchors[0];
            var nextAnchor = Anchors[1];
            foreach (var anchor in Anchors) {
                if (anchor.Pos.X < x) {
                    previousAnchor = anchor;
                } else {
                    nextAnchor = anchor;
                    break;
                }
            }

            // Calculate the value via interpolation
            var diff = nextAnchor.Pos - previousAnchor.Pos;
            if (Math.Abs(diff.X) < Precision.DOUBLE_EPSILON) {
                return previousAnchor.Pos.Y;
            }
            var sectionProgress = (x - previousAnchor.Pos.X) / diff.X;

            var interpolator = nextAnchor.Interpolator;
            interpolator.P = nextAnchor.Tension;

            return previousAnchor.Pos.Y + diff.Y * interpolator.GetInterpolation(sectionProgress);
        }

        /// <summary>
        /// Converts a value from the coordinate system to the absolute coordinate system [0-1]x[0-1].
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Vector2 GetPosition(Vector2 value) {
            return new Vector2((value.X - MinX) / (MaxX - MinX), (value.Y - MinY) / (MaxY - MinY));
        }

        /// <summary>
        /// Calculates the height of the curve from <see cref="MinY"/> to <see cref="MaxY"/> for a given progression allong the graph [0-1].
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double GetValue(double x) {
            return MinY + (MaxY - MinY) * GetPosition(x);
        }

        /// <summary>
        /// Converts a value from the absolute coordinate system to a value.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 GetValue(Vector2 position) {
            return new Vector2(MinX + (MaxX - MinX) * position.X, MinY + (MaxY - MinY) * position.Y);
        }

        #endregion

        #region ChangeEventHandlers

        private static void OnAnchorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateVisual();
            g.GraphStateChanged?.Invoke(d, e);
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.RegenerateMarkers();
            g.GraphStateChanged?.Invoke(d, e);
        }

        private static void OnMarkerGeneratorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.RegenerateMarkers();
        }

        private static void OnMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateMarkers();
            g.UpdateVisual();
        }

        private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateVisual();
        }

        private static void OnEdgesBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g._markers.ForEach(o => o.Stroke = g.EdgesBrush);
            g.UpdateVisual();
        }

        private static void OnAnchorStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            foreach (var anchor in g.Anchors) {
                anchor.Stroke = g.AnchorStroke;
            }
            g.UpdateVisual();
        }

        private static void OnAnchorFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            foreach (var anchor in g.Anchors) {
                anchor.Fill = g.AnchorFill;
            }
            g.UpdateVisual();
        }

        private static void OnTensionAnchorStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            foreach (var anchor in g.TensionAnchors) {
                anchor.Stroke = g.TensionAnchorStroke;
            }
            g.UpdateVisual();
        }

        private static void OnTensionAnchorFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            foreach (var anchor in g.TensionAnchors) {
                anchor.Fill = g.TensionAnchorFill;
            }
            g.UpdateVisual();
        }

        #endregion

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

        public void RegenerateMarkers() {
            _markers.Clear();
            if (HorizontalMarkerGenerator != null)
                _markers.AddRange(HorizontalMarkerGenerator.GenerateMarkers(MinX, MaxX, Orientation.Vertical));
            if (VerticalMarkerGenerator != null)
                _markers.AddRange(VerticalMarkerGenerator.GenerateMarkers(MinX, MaxX, Orientation.Horizontal));

            UpdateMarkers();
            UpdateVisual();
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var newAnchor = AddAnchor(GetPosition(e.GetPosition(this)));
            newAnchor.EnableDragging();
        }

        private Point GetRelativePoint(Vector2 pos) {
            return new Point(pos.X * ActualWidth, ActualHeight - pos.Y * ActualHeight);
        }

        private Point GetRelativePoint(double x) {
            return GetRelativePoint(new Vector2(x, GetPosition(x)));
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
            foreach (var marker in _markers.Where(marker => !(marker.X < 0) && !(marker.X > ActualWidth) && !(marker.Y < 0) && !(marker.Y > ActualHeight))) {
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
            if (Anchors.Count < 2) return;

            // Calculate interpolation line
            var points = new PointCollection();
            for (int i = 1; i < Anchors.Count; i++) {
                var previous = Anchors[i - 1];
                var next = Anchors[i];

                points.Add(GetRelativePoint(previous.Pos));

                for (int k = 1; k < ActualWidth * (next.Pos.X - previous.Pos.X); k++) {
                    var x = previous.Pos.X + k / ActualWidth;

                    points.Add(GetRelativePoint(x));
                }
            }
            points.Add(GetRelativePoint(Anchors[Anchors.Count - 1].Pos));

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
            foreach (var tensionAnchor in TensionAnchors) {
                // Find x position in the middle
                var next = tensionAnchor.ParentAnchor;
                var previous = Anchors[Anchors.IndexOf(next) - 1];

                if (Math.Abs(next.Pos.X - previous.Pos.X) < Precision.DOUBLE_EPSILON) {
                    continue;
                }
                var x = (next.Pos.X + previous.Pos.X) / 2;

                // Get y on the graph and set position
                var y = GetPosition(x);
                tensionAnchor.Pos = new Vector2(x, y);

                RenderGraphPoint(tensionAnchor);
            }

            // Add State.Anchors
            foreach (var anchor in Anchors) {
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
            var index = Anchors.IndexOf(anchor);
            var previous = Anchors.ElementAtOrDefault(index - 1);
            var next = Anchors.ElementAtOrDefault(index + 1);
            if (previous == null || next == null) {
                // Is edge anchor so dont move it
                pos.X = anchor.Pos.X;
            } else {
                // Snap to nearest vertical marker unless left alt is held
                if (!Keyboard.IsKeyDown(Key.LeftAlt)) {
                    // Find the nearest marker
                    GraphMarker nearestMarker = null;
                    double nearestDistance = double.PositiveInfinity;
                    foreach (var marker in _markers.Where(o => o.Orientation == Orientation.Vertical)) {
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

        private void UpdateMarkers() {
            var prevHorizontal = double.NegativeInfinity;
            var prevVertical = double.NegativeInfinity;
            foreach (var graphMarker in _markers) {
                graphMarker.Stroke = EdgesBrush;
                graphMarker.Width = ActualWidth;
                graphMarker.Height = ActualHeight;
                if (graphMarker.Orientation == Orientation.Horizontal) {
                    graphMarker.X = 0;
                    graphMarker.Y = ActualHeight - ActualHeight * ((graphMarker.Value - MinY) / (MaxY - MinY));
                    graphMarker.Visible = Math.Abs(prevHorizontal - graphMarker.Y) >= MinMarkerSpacing;
                    if (graphMarker.Visible) {
                        prevHorizontal = graphMarker.Y;
                    }
                } else {
                    graphMarker.X = ActualWidth * ((graphMarker.Value - MinX) / (MaxX - MinX));
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
