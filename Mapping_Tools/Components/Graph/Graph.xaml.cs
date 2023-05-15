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
        private bool initialized;
        private bool drawAnchors;
        private bool isDragging;
        private Point lastMousePoint;
        private readonly List<GraphMarker> markers;

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

        public static readonly DependencyProperty ViewMinXProperty =
            DependencyProperty.Register(nameof(ViewMinX),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnViewChanged));

        public double ViewMinX {
            get => (double)GetValue(ViewMinXProperty);
            set => SetValue(ViewMinXProperty, value);
        }

        public static readonly DependencyProperty ViewMinYProperty =
            DependencyProperty.Register(nameof(ViewMinY),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnViewChanged));

        public double ViewMinY {
            get => (double)GetValue(ViewMinYProperty);
            set => SetValue(ViewMinYProperty, value);
        }

        public static readonly DependencyProperty ViewMaxXProperty =
            DependencyProperty.Register(nameof(ViewMaxX),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.None,
                    OnViewChanged));

        public double ViewMaxX {
            get => (double)GetValue(ViewMaxXProperty);
            set => SetValue(ViewMaxXProperty, value);
        }

        public static readonly DependencyProperty ViewMaxYProperty =
            DependencyProperty.Register(nameof(ViewMaxY),
                typeof(double),
                typeof(Graph),
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.None,
                    OnViewChanged));

        public double ViewMaxY {
            get => (double)GetValue(ViewMaxYProperty);
            set => SetValue(ViewMaxYProperty, value);
        }

        public double ViewWidth => ViewMaxX - ViewMinX;

        public double ViewHeight => ViewMaxY - ViewMinY;

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
            
            markers = new List<GraphMarker>();
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

            initialized = true;
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
            ResetView();
            Anchors = new AnchorCollection(graphState.Anchors.Select(a => a.GetAnchor()));
        }

        public void ResetView() {
            ViewMinX = MinX;
            ViewMinY = MinY;
            ViewMaxX = MaxX;
            ViewMaxY = MaxY;
        }

        private void SetCursor() {
            if (ViewMinX == MinX && ViewMinY == MinY && ViewMaxX == MaxX && ViewMaxY == MaxY) {
                Cursor = Cursors.Arrow;
            } else {
                Cursor = Cursors.SizeAll;
            }
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
            ResetView();

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
            return new Point(GetRelativePointX(value.X), GetRelativePointY(value.Y));
        }

        public double GetRelativePointX(double valueX) {
            return (valueX - ViewMinX) / ViewWidth * ActualWidth;
        }

        public double GetRelativePointY(double valueY) {
            return  ActualHeight - (valueY - ViewMinY) / ViewHeight * ActualHeight;
        }

        public Vector2 GetValue(Point pos) {
            return new Vector2(GetValueX(pos.X), GetValueY(pos.Y));
        }

        public Vector2 GetValueVector(Point pos) {
            var t = new Vector2(pos.X / ActualWidth, -pos.Y / ActualHeight);
            return new Vector2(ViewWidth * t.X, ViewHeight * t.Y);
        }

        public double GetValueX(double pointX) {
            return ViewMinX + ViewWidth * (pointX / ActualWidth);
        }

        public double GetValueY(double pointY) {
            return ViewMinY + ViewHeight * ((ActualHeight - pointY) / ActualHeight);
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
            if (g.ScaleOnBoundChangeHorizontal || g.ScaleOnBoundChangeVertical) {
                foreach (var anchor in g.Anchors) {
                    anchor.Pos = new Vector2(g.ScaleOnBoundChangeHorizontal ? g.MinX + (g.MaxX - g.MinX) * (anchor.Pos.X - oldMinX) / (oldMaxX - oldMinX) : anchor.Pos.X,
                                             g.ScaleOnBoundChangeVertical ? g.MinY + (g.MaxY - g.MinY) * (anchor.Pos.Y - oldMinY) / (oldMaxY - oldMinY) : anchor.Pos.Y);
                }
            }
            g.ResetView();
            g.RegenerateMarkers();
        }

        private static void OnViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var g = (Graph)d;
            g.SetCursor();
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
            g.markers.ForEach(o => o.Stroke = g.EdgesBrush);
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
            markers.Clear();
            if (HorizontalMarkerGenerator != null)
                markers.AddRange(HorizontalMarkerGenerator.GenerateMarkers(ViewMinX, ViewMaxX, Orientation.Vertical,
                    (int)(ActualWidth / MinMarkerSpacing)));
            if (VerticalMarkerGenerator != null)
                markers.AddRange(VerticalMarkerGenerator.GenerateMarkers(ViewMinY, ViewMaxY, Orientation.Horizontal,
                    (int)(ActualHeight / MinMarkerSpacing)));

            UpdateMarkers();
            UpdateVisual();
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var newAnchor = AddAnchor(GetValue(e.GetPosition(this)));
            newAnchor.EnableDragging();
        }

        public void UpdateVisual() {
            if (!initialized) return;

            // Clear canvas
            MainCanvas.Children.Clear();

            // Add markers
            foreach (var marker in markers.Concat(ExtraMarkers).Where(marker => marker.X > -Precision.DoubleEpsilon && 
                                                            marker.X < ActualWidth + Precision.DoubleEpsilon && 
                                                            marker.Y > -Precision.DoubleEpsilon && 
                                                            marker.Y < ActualHeight + Precision.DoubleEpsilon)) {
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

            // Get the clamping bounds of the view
            var b1 = new Vector2(ViewMinX, ViewMinY);
            var b2 = new Vector2(ViewMaxX, ViewMaxY);

            // Calculate interpolation line
            var points = new PointCollection();
            for (int i = 1; i < Anchors.Count; i++) {
                if (ViewWidth <= 0) continue;

                var previous = Anchors[i - 1];
                var next = Anchors[i];

                if (next.Pos.X < ViewMinX || previous.Pos.X > ViewMaxX) {
                    continue;
                }

                var previousPos = Vector2.Clamp(previous.Pos, b1, b2);
                var nextPos = Vector2.Clamp(next.Pos, b1, b2);

                var previousPoint = GetRelativePoint(previousPos);
                var nextPoint = GetRelativePoint(nextPos);

                if (previous.Pos.X >= ViewMinX && previous.Pos.X <= ViewMaxX) {
                    points.Add(previousPoint);
                }

                var maxPoints = Math.Min(1000, nextPoint.X - previousPoint.X);
                var width = nextPos.X - previousPos.X;
                var d = width / maxPoints;
                var start = previous.Pos.X >= ViewMinX && previous.Pos.X <= ViewMaxX ? 1 : 0;
                var end = next.Pos.X >= ViewMinX && next.Pos.X <= ViewMaxX ? maxPoints - 1 : maxPoints;
                for (int k = start; k <= end; k++) {
                    var x = previousPos.X + k * d;

                    if (x + d < ViewMinX || x - d > ViewMaxX)
                        continue;

                    x = Math.Clamp(x, ViewMinX, ViewMaxX);
                    var p = Vector2.Clamp(new Vector2(x, GetValue(x)), b1, b2);

                    points.Add(GetRelativePoint(p));
                }
            }
            if (Anchors[^1].Pos.X >= ViewMinX && Anchors[^1].Pos.X <= ViewMaxX) {
                points.Add(GetRelativePoint(Vector2.Clamp(Anchors[^1].Pos, b1, b2)));
            }

            // Draw line
            var line = new Polyline {Points = points, Stroke = Stroke, StrokeThickness = 2, IsHitTestVisible = false,
                StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round};
            MainCanvas.Children.Add(line);

            // Draw area under line
            var points2 = new PointCollection(points) {
                GetRelativePoint(Vector2.Clamp(new Vector2(Anchors[Anchors.Count - 1].Pos.X, VerticalAxis), b1, b2)),
                GetRelativePoint(Vector2.Clamp(new Vector2(Anchors[0].Pos.X, VerticalAxis), b1, b2))
            };

            var polygon = new Polygon {Points = points2, Fill = Fill, IsHitTestVisible = false};
            MainCanvas.Children.Add(polygon);

            // Return if we dont draw Anchors or if the graph is not user editable. Having invisible anchors makes it impossible to edit
            if (!drawAnchors || !UserEditable) return;
            
            // Add tension Anchors
            foreach (var anchor in Anchors) {
                // Find x position in the middle
                var next = anchor;
                var previous = anchor.PreviousAnchor;

                if (previous == null || Math.Abs(next.Pos.X - previous.Pos.X) < Precision.DoubleEpsilon) {
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
            if (point.Pos.X < ViewMinX || point.Pos.X > ViewMaxX ||
                point.Pos.Y < ViewMinY || point.Pos.Y > ViewMaxY) {
                return;
            }

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
                    foreach (var marker in markers.Concat(ExtraMarkers).Where(o => o.Orientation == Orientation.Vertical && o.Snappable)) {
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
                    foreach (var marker in markers.Concat(ExtraMarkers).Where(o => o.Orientation == Orientation.Horizontal && o.Snappable)) {
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
            if (drawAnchors) return;
            drawAnchors = true;
            UpdateVisual();
        }

        private void Graph_OnMouseLeave(object sender, MouseEventArgs e) {
            if (!drawAnchors) return;
            drawAnchors = false;
            UpdateVisual();
        }

        private void Graph_OnMouseWheel(object sender, MouseWheelEventArgs e) {
            var zoomPoint = GetValue(e.GetPosition(this));

            if (zoomPoint.X < 0 || zoomPoint.Y < 0 || zoomPoint.X > MaxX || zoomPoint.Y > MaxY)
                return;

            var scale = Math.Pow(2, -e.Delta / 240d);
            var scaleX = Keyboard.IsKeyDown(Key.LeftCtrl) ? 1 : scale;
            var scaleY = Keyboard.IsKeyDown(Key.LeftShift) ? 1 : scale;

            // Get new view box
            var v1 = new Vector2(
                (ViewMinX - zoomPoint.X) * scaleX,
                (ViewMinY - zoomPoint.Y) * scaleY) + zoomPoint;
            var v2 = new Vector2(
                (ViewMaxX - zoomPoint.X) * scaleX,
                (ViewMaxY - zoomPoint.Y) * scaleY) + zoomPoint;

            // Clamp into bounds of graph
            var b1 = new Vector2(MinX, MinY);
            var b2 = new Vector2(MaxX, MaxY);

            v1 = Vector2.Clamp(v1, b1, b2);
            v2 = Vector2.Clamp(v2, b1, b2);

            ViewMinX = v1.X;
            ViewMinY = v1.Y;
            ViewMaxX = v2.X;
            ViewMaxY = v2.Y;

            RegenerateMarkers();
        }

        private void UpdateMarkers() {
            foreach (var graphMarker in markers.Concat(ExtraMarkers)) {
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

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            lastMousePoint = e.GetPosition(this);

            CaptureMouse();
            isDragging = true;
            e.Handled = true;
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            ReleaseMouseCapture();
            isDragging = false;
            e.Handled = true;
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e) {
            if (!isDragging) return;

            if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
                ReleaseMouseCapture();
                isDragging = false;
                return;
            }

            // Get the position of the mouse relative to the Canvas
            var newMousePoint = e.GetPosition(this);
            var diff = lastMousePoint - newMousePoint;
            lastMousePoint = newMousePoint;

            // Move the view box by diff
            var valueDiff = GetValueVector(new Point(diff.X, diff.Y));

            var v1 = new Vector2(ViewMinX, ViewMinY);
            var v2 = new Vector2(ViewMaxX, ViewMaxY);
            var b1 = new Vector2(MinX, MinY);
            var b2 = new Vector2(MaxX, MaxY);

            valueDiff = Vector2.Clamp(valueDiff, b1 - v1, b2 - v2);

            ViewMinX += valueDiff.X;
            ViewMinY += valueDiff.Y;
            ViewMaxX += valueDiff.X;
            ViewMaxY += valueDiff.Y;

            e.Handled = true;
        }
    }
}
