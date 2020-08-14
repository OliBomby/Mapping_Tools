using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using Mapping_Tools.Components.Graph.Markers;
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

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph {
        private bool _initialized;
        private bool _drawAnchors;
        private readonly List<GraphMarker> _markers;

        public bool IgnoreAnchorUpdates { get; set; }

        #region DependencyProperties

        public static readonly DependencyProperty AnchorsProperty =
            DependencyProperty.Register(nameof(Anchors),
                typeof(AnchorCollection), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnAnchorsChanged));

        public AnchorCollection Anchors {
            get => (AnchorCollection) GetValue(AnchorsProperty);
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

        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.Register(nameof(HorizontalAxis),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnAxisChanged));

        public double HorizontalAxis {
            get => (double) GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public static readonly DependencyProperty VerticalAxisProperty =
            DependencyProperty.Register(nameof(VerticalAxis),
                typeof(double), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnAxisChanged));

        public double VerticalAxis {
            get => (double) GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public static readonly DependencyProperty VerticalAxisVisibleProperty =
            DependencyProperty.Register(nameof(VerticalAxisVisible),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None,
                    OnAxisChanged));

        public bool VerticalAxisVisible {
            get => (bool) GetValue(VerticalAxisVisibleProperty);
            set => SetValue(VerticalAxisVisibleProperty, value);
        }

        public static readonly DependencyProperty HorizontalAxisVisibleProperty =
            DependencyProperty.Register(nameof(HorizontalAxisVisible),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None,
                    OnAxisChanged));

        public bool HorizontalAxisVisible {
            get => (bool) GetValue(HorizontalAxisVisibleProperty);
            set => SetValue(HorizontalAxisVisibleProperty, value);
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

        public static readonly DependencyProperty ExtraMarkersProperty =
            DependencyProperty.Register(nameof(ExtraMarkers),
                typeof(ObservableCollection<GraphMarker>), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnExtraMarkersChanged));

        public ObservableCollection<GraphMarker> ExtraMarkers {
            get => (ObservableCollection<GraphMarker>) GetValue(ExtraMarkersProperty);
            set => SetValue(ExtraMarkersProperty, value);
        }

        public static readonly DependencyProperty UserEditableProperty =
            DependencyProperty.Register(nameof(UserEditable),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.None,
                    OnEditableChanged));

        public bool UserEditable {
            get => (bool) GetValue(UserEditableProperty);
            set => SetValue(UserEditableProperty, value);
        }

        public static readonly DependencyProperty StartPointLockedXProperty =
            DependencyProperty.Register(nameof(StartPointLockedX),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.None));

        public bool StartPointLockedX {
            get => (bool) GetValue(StartPointLockedXProperty);
            set => SetValue(StartPointLockedXProperty, value);
        }

        public static readonly DependencyProperty StartPointLockedYProperty =
            DependencyProperty.Register(nameof(StartPointLockedY),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public bool StartPointLockedY {
            get => (bool) GetValue(StartPointLockedYProperty);
            set => SetValue(StartPointLockedYProperty, value);
        }

        public static readonly DependencyProperty EndPointLockedXProperty =
            DependencyProperty.Register(nameof(EndPointLockedX),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.None));

        public bool EndPointLockedX {
            get => (bool) GetValue(EndPointLockedXProperty);
            set => SetValue(EndPointLockedXProperty, value);
        }

        public static readonly DependencyProperty EndPointLockedYProperty =
            DependencyProperty.Register(nameof(EndPointLockedY),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public bool EndPointLockedY {
            get => (bool) GetValue(EndPointLockedYProperty);
            set => SetValue(EndPointLockedYProperty, value);
        }

        public static readonly DependencyProperty MarkerSnappingHorizontalProperty =
            DependencyProperty.Register(nameof(MarkerSnappingHorizontal),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public bool MarkerSnappingHorizontal {
            get => (bool) GetValue(MarkerSnappingHorizontalProperty);
            set => SetValue(MarkerSnappingHorizontalProperty, value);
        }

        public static readonly DependencyProperty MarkerSnappingVerticalProperty =
            DependencyProperty.Register(nameof(MarkerSnappingVertical),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public bool MarkerSnappingVertical {
            get => (bool) GetValue(MarkerSnappingVerticalProperty);
            set => SetValue(MarkerSnappingVerticalProperty, value);
        }

        public static readonly DependencyProperty MarkerSnappingRangeHorizontalProperty =
            DependencyProperty.Register(nameof(MarkerSnappingRangeHorizontal),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.None));

        public double MarkerSnappingRangeHorizontal {
            get => (double)GetValue(MarkerSnappingRangeHorizontalProperty);
            set => SetValue(MarkerSnappingRangeHorizontalProperty, value);
        }

        public static readonly DependencyProperty MarkerSnappingRangeVerticalProperty =
            DependencyProperty.Register(nameof(MarkerSnappingRangeVertical),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.None));

        public double MarkerSnappingRangeVertical {
            get => (double)GetValue(MarkerSnappingRangeVerticalProperty);
            set => SetValue(MarkerSnappingRangeVerticalProperty, value);
        }

        public static readonly DependencyProperty ScaleOnBoundChangeHorizontalProperty =
            DependencyProperty.Register(nameof(ScaleOnBoundChangeHorizontal),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.None));

        public bool ScaleOnBoundChangeHorizontal {
            get => (bool) GetValue(ScaleOnBoundChangeHorizontalProperty);
            set => SetValue(ScaleOnBoundChangeHorizontalProperty, value);
        }

        public static readonly DependencyProperty ScaleOnBoundChangeVerticalProperty =
            DependencyProperty.Register(nameof(ScaleOnBoundChangeVertical),
                typeof(bool), 
                typeof(Graph), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public bool ScaleOnBoundChangeVertical {
            get => (bool) GetValue(ScaleOnBoundChangeVerticalProperty);
            set => SetValue(ScaleOnBoundChangeVerticalProperty, value);
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
            Anchors = new AnchorCollection();
            ExtraMarkers = new ObservableCollection<GraphMarker>();
            LastInterpolationSet = typeof(SingleCurveInterpolator);

            Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            EdgesBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

            // Initialize Anchors
            Anchors.Add(MakeAnchor(new Vector2(0, 0.5)));
            AddAnchor(new Vector2(1, 0.5));

            Anchors.CollectionChanged += AnchorsOnCollectionChanged;
            Anchors.AnchorsChanged += AnchorsOnAnchorsChanged;
            ExtraMarkers.CollectionChanged += ExtraMarkersOnCollectionChanged;

            _initialized = true;
            UpdateVisual();
        }

        private void ExtraMarkersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateMarkers();
            UpdateVisual();
        }

        private void AnchorsOnAnchorsChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (IgnoreAnchorUpdates) return;
            UpdateVisual();
        }

        private void AnchorsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (IgnoreAnchorUpdates) return;
            UpdateVisual();
        }

        /// <summary>
        /// Gets the freezable state of the graph.
        /// </summary>
        /// <returns></returns>
        public GraphState GetGraphState() {
            return new GraphState {
                Anchors = Anchors.Select(a => a.GetAnchorState()).ToList(),
                MinX = MinX, MinY = MinY, MaxX = MaxX, MaxY = MaxY
            };
        }

        public void SetGraphState(GraphState graphState) {
            MinX = graphState.MinX;
            MinY = graphState.MinY;
            MaxX = graphState.MaxX;
            MaxY = graphState.MaxY;
            Anchors = new AnchorCollection(graphState.Anchors.Select(a => a.GetAnchor()));
        }

        #region GraphStuff

        public Anchor AddAnchor(Vector2 pos) {
            // Clamp the position withing bounds
            pos = Vector2.Clamp(pos, new Vector2(MinX, MinY), new Vector2(MaxX, MaxY));

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

            // Add tension
            anchor.Tension = nextAnchor?.Tension ?? 0;
            
            // Insert anchor
            Anchors.Insert(index, anchor);

            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos) {
            var anchor = new Anchor(this, pos);
            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos, Type lastInterpolatorSet) {
            var anchor = new Anchor(this, pos, InterpolatorHelper.GetInterpolator(lastInterpolatorSet));
            return anchor;
        }

        public void RemoveAnchor(Anchor anchor) {
            // Dont remove the anchors on the left and right edge
            if (IsEdgeAnchor(anchor)) return;

            Anchors.Remove(anchor);
        }

        public void RemoveAnchorAt(int index) {
            if (index <= 0 || index >= Anchors.Count - 1) return;
            
            Anchors.RemoveAt(index);
        }
        
        /// <summary>
        /// Removes all anchors between the first and last anchor and resets all tension values.
        /// </summary>
        public void Clear() {
            for (int i = 1; i < Anchors.Count - 1; ) {
                RemoveAnchorAt(i);
            }

            foreach (var anchor in Anchors) {
                // Remove NaNs from the position
                if (double.IsNaN(anchor.Pos.X)) {
                    anchor.Pos = new Vector2(0, anchor.Pos.Y);
                }
                if (double.IsNaN(anchor.Pos.Y)) {
                    anchor.Pos = new Vector2(anchor.Pos.X, 0);
                }
                anchor.Pos = Vector2.Clamp(anchor.Pos, new Vector2(MinX, MinY), new Vector2(MaxX, MaxY));
                anchor.SetTension(0);
            }
        }

        /// <summary>
        /// Multiplies the positions of all anchors by a X-scalar and a Y-scalar.
        /// </summary>
        /// <param name="scalar"></param>
        public void ScaleAnchors(Size scalar) {
            if (double.IsNaN(scalar.Width) || double.IsNaN(scalar.Height)) return;

            foreach (var anchor in Anchors) {
                anchor.Pos = new Vector2(anchor.Pos.X * scalar.Width, anchor.Pos.Y * scalar.Height);
            }

            UpdateVisual();
        }

        public bool IsEdgeAnchor(Anchor anchor) {
            return anchor == Anchors[0] || anchor == Anchors[Anchors.Count - 1];
        }

        public Point GetRelativePoint(Vector2 value) {
            var t = new Vector2((value.X - MinX) / (MaxX - MinX), (value.Y - MinY) / (MaxY - MinY));
            return new Point(t.X * ActualWidth, ActualHeight - t.Y * ActualHeight);
        }

        public double GetRelativePointX(double valueX) {
            return (valueX - MinX) / (MaxX - MinX) * ActualWidth;
        }

        public double GetRelativePointY(double valueY) {
            return  ActualHeight - (valueY - MinY) / (MaxY - MinY) * ActualHeight;
        }

        public Vector2 GetValue(Point pos) {
            var t = new Vector2(pos.X / ActualWidth, (ActualHeight - pos.Y) / ActualHeight);
            return new Vector2(MinX + (MaxX - MinX) * t.X, MinY + (MaxY - MinY) * t.Y);
        }

        public Vector2 GetValueRelative(Point pos) {
            var t = new Vector2(pos.X / ActualWidth, - pos.Y / ActualHeight);
            return new Vector2((MaxX - MinX) * t.X,  (MaxY - MinY) * t.Y);
        }

        public double GetValueX(double pointX) {
            return MinX + (MaxX - MinX) * (pointX / ActualWidth);
        }

        public double GetValueY(double pointY) {
            return MinY + (MaxY - MinY) * ((ActualHeight - pointY) / ActualHeight);
        }

        public Vector2 GetValue(GraphMarker marker) {
            return GetValue(new Point(marker.X, marker.Y));
        }

        /// <summary>
        /// Calculates the height of the curve [0-1] for a given progression along the graph [0-1].
        /// </summary>
        /// <param name="x">The progression along the curve (0-1)</param>
        /// <returns>The height of the curve (0-1)</returns>
        public double GetValue(double x) {
            return Anchors.GetValue(x);
        }

        public double GetDerivative(double x) {
            return Anchors.GetDerivative(x);
        }

        public double GetIntegral(double t1, double t2) {
            return Anchors.GetIntegral(t1, t2);
        }

        #endregion

        #region ChangeEventHandlers

        private static void OnAnchorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.Anchors.UpdateAnchorNeighbors();
            foreach (var anchor in g.Anchors) {
                anchor.Graph = g;
                anchor.TensionAnchor.ParentAnchor = anchor;
                anchor.TensionAnchor.Graph = g;
                anchor.Stroke = g.AnchorStroke;
                anchor.Fill = g.AnchorFill;
                anchor.TensionAnchor.Stroke = g.TensionAnchorStroke;
                anchor.TensionAnchor.Fill = g.TensionAnchorFill;
            }
            
            g.Anchors.CollectionChanged += g.AnchorsOnCollectionChanged;
            g.Anchors.AnchorsChanged += g.AnchorsOnAnchorsChanged;

            g.UpdateVisual();
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            var oldMinX = g.MinX;
            var oldMaxX = g.MaxX;
            var oldMinY = g.MinY;
            var oldMaxY = g.MaxY;
            switch (e.Property.Name) {
                case nameof(MinX):
                    oldMinX = (double) e.OldValue;
                    break;
                case nameof(MaxX):
                    oldMaxX = (double) e.OldValue;
                    break;
                case nameof(MinY):
                    oldMinY = (double) e.OldValue;
                    break;
                case nameof(MaxY):
                    oldMaxY = (double) e.OldValue;
                    break;
            }
            foreach (var anchor in g.Anchors) {
                anchor.Pos = new Vector2(g.ScaleOnBoundChangeHorizontal ? g.MinX + (g.MaxX - g.MinX) * (anchor.Pos.X - oldMinX) / (oldMaxX - oldMinX) : anchor.Pos.X,
                                         g.ScaleOnBoundChangeVertical ? g.MinY + (g.MaxY - g.MinY) * (anchor.Pos.Y - oldMinY) / (oldMaxY - oldMinY) : anchor.Pos.Y);
            }
            g.RegenerateMarkers();
        }

        private static void OnAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateVisual();
        }

        private static void OnMarkerGeneratorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.RegenerateMarkers();
        }

        private static void OnEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateVisual();
        }

        private static void OnMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.UpdateMarkers();
            g.UpdateVisual();
        }

        private static void OnExtraMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            g.ExtraMarkers.CollectionChanged += g.ExtraMarkersOnCollectionChanged;
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
            foreach (var anchor in g.Anchors) {
                anchor.TensionAnchor.Stroke = g.TensionAnchorStroke;
            }
            g.UpdateVisual();
        }

        private static void OnTensionAnchorFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph) d;
            foreach (var anchor in g.Anchors) {
                anchor.TensionAnchor.Fill = g.TensionAnchorFill;
            }
            g.UpdateVisual();
        }

        #endregion

        /// <summary>
        /// Instantly sets all brushes to one specified colour theme.
        /// </summary>
        /// <param name="brush"></param>
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
                _markers.AddRange(HorizontalMarkerGenerator.GenerateMarkers(MinX, MaxX, Orientation.Vertical,
                    (int)(ActualWidth / MinMarkerSpacing)));
            if (VerticalMarkerGenerator != null)
                _markers.AddRange(VerticalMarkerGenerator.GenerateMarkers(MinY, MaxY, Orientation.Horizontal,
                    (int)(ActualHeight / MinMarkerSpacing)));

            UpdateMarkers();
            UpdateVisual();
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var newAnchor = AddAnchor(GetValue(e.GetPosition(this)));
            newAnchor.EnableDragging();
        }

        public void UpdateVisual() {
            if (!_initialized) return;

            // Clear canvas
            MainCanvas.Children.Clear();

            // Add markers
            foreach (var marker in _markers.Concat(ExtraMarkers).Where(marker => marker.X > -Precision.DOUBLE_EPSILON && 
                                                            marker.X < ActualWidth + Precision.DOUBLE_EPSILON && 
                                                            marker.Y > -Precision.DOUBLE_EPSILON && 
                                                            marker.Y < ActualHeight + Precision.DOUBLE_EPSILON)) {
                MainCanvas.Children.Add(marker);
            }

            // Add axis
            if (HorizontalAxisVisible) {
                var y = GetRelativePointY(HorizontalAxis);
                var  horizontalLine = new Line {
                    Stroke = EdgesBrush, StrokeThickness = 3, X1 = 0, X2 = ActualWidth, Y1 = y, Y2 = y
                };
                MainCanvas.Children.Add(horizontalLine);
            }
            if (VerticalAxisVisible) {
                var x = GetRelativePointX(VerticalAxis);
                var  verticalLine = new Line {
                    Stroke = EdgesBrush, StrokeThickness = 3, X1 = x, X2 = x, Y1 = 0, Y2 = ActualHeight
                };
                MainCanvas.Children.Add(verticalLine);
            }

            // Add border
            var rect = new Rectangle {
                Stroke = EdgesBrush, Width = ActualWidth, Height = ActualHeight, StrokeThickness = 2
            };
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            MainCanvas.Children.Add(rect);

            // Check if there are at least 2 anchors
            if (Anchors == null || Anchors.Count < 2) return;

            // Calculate interpolation line
            var points = new PointCollection();
            for (int i = 1; i < Anchors.Count; i++) {
                var previous = Anchors[i - 1];
                var next = Anchors[i];

                points.Add(GetRelativePoint(previous.Pos));

                for (int k = 1; k < GetRelativePointX(next.Pos.X) - GetRelativePointX(previous.Pos.X); k++) {
                    var x = previous.Pos.X + k / ActualWidth * (MaxX - MinX);

                    points.Add(GetRelativePoint(new Vector2(x, GetValue(x))));
                }
            }
            points.Add(GetRelativePoint(Anchors[Anchors.Count - 1].Pos));
            
            // Draw line
            var line = new Polyline {Points = points, Stroke = Stroke, StrokeThickness = 2, IsHitTestVisible = false,
                StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round};
            MainCanvas.Children.Add(line);

            // Draw area under line
            var points2 = new PointCollection(points) {
                GetRelativePoint(new Vector2(Anchors[Anchors.Count - 1].Pos.X, VerticalAxis)), GetRelativePoint(new Vector2(Anchors[0].Pos.X, VerticalAxis))
            };

            var polygon = new Polygon {Points = points2, Fill = Fill, IsHitTestVisible = false};
            MainCanvas.Children.Add(polygon);

            // Return if we dont draw Anchors or if the graph is not user editable. Having invisible anchors makes it impossible to edit
            if (!_drawAnchors || !UserEditable) return;
            
            // Add tension Anchors
            foreach (var anchor in Anchors) {
                // Find x position in the middle
                var next = anchor;
                var previous = anchor.PreviousAnchor;

                if (previous == null || Math.Abs(next.Pos.X - previous.Pos.X) < Precision.DOUBLE_EPSILON) {
                    continue;
                }
                var x = (next.Pos.X + previous.Pos.X) / 2;

                // Get y on the graph and set position
                var y = GetValue(x);
                anchor.TensionAnchor.Pos = new Vector2(x, y);

                RenderGraphPoint(anchor.TensionAnchor);
            }
            
            // Add Anchors
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

            // Snap to nearest marker unless left alt is held
            if (!Keyboard.IsKeyDown(Key.LeftAlt)) {
                if (MarkerSnappingHorizontal) {
                    // Find the nearest marker
                    GraphMarker nearestMarkerHorizontal = null;
                    double nearestDistance = double.PositiveInfinity;
                    foreach (var marker in _markers.Concat(ExtraMarkers).Where(o => o.Orientation == Orientation.Vertical && o.Snappable)) {
                        var markerPos = GetValue(marker);
                        var dist = Math.Abs(pos.X - markerPos.X);
                        if (!(dist < nearestDistance)) continue;
                        nearestDistance = dist;
                        nearestMarkerHorizontal = marker;
                    }
                    // Set X to that marker's value
                    if (nearestMarkerHorizontal != null && nearestDistance <= MarkerSnappingRangeHorizontal)
                        pos.X = GetValueX(nearestMarkerHorizontal.X);
                }
                if (MarkerSnappingVertical) {
                    // Find the nearest marker
                    GraphMarker nearestMarkerVertical = null;
                    double nearestDistance = double.PositiveInfinity;
                    foreach (var marker in _markers.Concat(ExtraMarkers).Where(o => o.Orientation == Orientation.Horizontal && o.Snappable)) {
                        var markerPos = GetValue(marker);
                        var dist = Math.Abs(pos.Y - markerPos.Y);
                        if (!(dist < nearestDistance)) continue;
                        nearestDistance = dist;
                        nearestMarkerVertical = marker;
                    }
                    // Set Y to that marker's value
                    if (nearestMarkerVertical != null && nearestDistance <= MarkerSnappingRangeVertical)
                        pos.Y = GetValueY(nearestMarkerVertical.Y);
                }
            }

            // Clip the new position between the previous and the next anchor
            if (previous != null) {
                pos.X = Math.Max(pos.X, previous.Pos.X);
            }
            if (next != null) {
                pos.X = Math.Min(pos.X, next.Pos.X);
            }

            // Clip the new Y position between the bounds of the graph
            pos.Y = MathHelper.Clamp(pos.Y, MinY, MaxY);
            
            // Handle lockedness of start/end point
            if (previous == null) {
                if (StartPointLockedX) {
                    pos.X = anchor.Pos.X;
                }
                if (StartPointLockedY) {
                    pos.Y = anchor.Pos.Y;
                }
            } else if (next == null) {
                if (EndPointLockedX) {
                    pos.X = anchor.Pos.X;
                }
                if (EndPointLockedY) {
                    pos.Y = anchor.Pos.Y;
                }
            }

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
            foreach (var graphMarker in _markers.Concat(ExtraMarkers)) {
                graphMarker.Stroke = EdgesBrush;
                graphMarker.Width = ActualWidth;
                graphMarker.Height = ActualHeight;
                if (graphMarker.Orientation == Orientation.Horizontal) {
                    graphMarker.X = 0;
                    graphMarker.Y = GetRelativePointY(graphMarker.Value);
                } else {
                    graphMarker.X = GetRelativePointX(graphMarker.Value);
                    graphMarker.Y = 0;
                }
                graphMarker.InvalidateVisual();
            }
        }

        private void Graph_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            RegenerateMarkers();
        }
    }
}
