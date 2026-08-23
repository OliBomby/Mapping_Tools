using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Mapping_Tools.Application.Interactions.Converters;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.Graph.Interpolation;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.Graph.Markers;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using CoreGraphState = Mapping_Tools.Core.Classes.Graph.GraphState;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Identifies the active pointer gesture in a graph editor.</summary>
public enum GraphPointerGesture
{
    /// <summary>No graph gesture is active.</summary>
    None,

    /// <summary>An anchor is being moved.</summary>
    Anchor,

    /// <summary>An interpolation tension handle is being moved.</summary>
    Tension,

    /// <summary>The graph viewport is being panned.</summary>
    Pan,
}

/// <summary>Provides the edited state after a graph gesture or menu operation.</summary>
public sealed class GraphStateChangedEventArgs : EventArgs
{
    /// <summary>Creates graph change information.</summary>
    /// <param name="state">The cloned state after the edit.</param>
    public GraphStateChangedEventArgs(CoreGraphState state)
    {
        State = state;
    }

    /// <summary>Gets the cloned state after the edit.</summary>
    public CoreGraphState State { get; }
}

/// <summary>
///     Draws and edits a normalized value graph while keeping graph mathematics in Core.
/// </summary>
public sealed class GraphControl : Control
{
    private const double MinimumViewSize = 1e-9;
    private const double AnchorHitRadius = 10;
    private const double TensionHitRadius = 9;
    private const int MaximumCurveSamples = 1000;

    /// <summary>Identifies the graph state edited by the control.</summary>
    public static readonly StyledProperty<CoreGraphState?> GraphStateProperty =
        AvaloniaProperty.Register<GraphControl, CoreGraphState?>(
            nameof(GraphState),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Identifies the interpolator copied to newly inserted anchors.</summary>
    public static readonly StyledProperty<Type> LastInterpolationSetProperty =
        AvaloniaProperty.Register<GraphControl, Type>(nameof(LastInterpolationSet), typeof(SingleCurveInterpolator));

    /// <summary>Identifies extra graph markers supplied by a host feature.</summary>
    public static readonly StyledProperty<IReadOnlyList<GraphMarker>> MarkersProperty =
        AvaloniaProperty.Register<GraphControl, IReadOnlyList<GraphMarker>>(
            nameof(Markers),
            Array.Empty<GraphMarker>());

    /// <summary>Identifies the generator for markers on the graph X axis.</summary>
    public static readonly StyledProperty<IGraphMarkerGenerator?> HorizontalMarkerGeneratorProperty =
        AvaloniaProperty.Register<GraphControl, IGraphMarkerGenerator?>(nameof(HorizontalMarkerGenerator));

    /// <summary>Identifies the generator for markers on the graph Y axis.</summary>
    public static readonly StyledProperty<IGraphMarkerGenerator?> VerticalMarkerGeneratorProperty =
        AvaloniaProperty.Register<GraphControl, IGraphMarkerGenerator?>(nameof(VerticalMarkerGenerator));

    /// <summary>Identifies the minimum distance between generated markers in pixels.</summary>
    public static readonly StyledProperty<double> MinMarkerSpacingProperty =
        AvaloniaProperty.Register<GraphControl, double>(nameof(MinMarkerSpacing), 10);

    /// <summary>Identifies the graph X value represented by the horizontal axis.</summary>
    public static readonly StyledProperty<double> HorizontalAxisProperty =
        AvaloniaProperty.Register<GraphControl, double>(nameof(HorizontalAxis));

    /// <summary>Identifies the graph Y value represented by the vertical axis.</summary>
    public static readonly StyledProperty<double> VerticalAxisProperty =
        AvaloniaProperty.Register<GraphControl, double>(nameof(VerticalAxis));

    /// <summary>Identifies whether the horizontal axis is drawn.</summary>
    public static readonly StyledProperty<bool> HorizontalAxisVisibleProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(HorizontalAxisVisible));

    /// <summary>Identifies whether the vertical axis is drawn.</summary>
    public static readonly StyledProperty<bool> VerticalAxisVisibleProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(VerticalAxisVisible));

    /// <summary>Identifies whether the control accepts anchor and tension edits.</summary>
    public static readonly StyledProperty<bool> IsEditableProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(IsEditable), true);

    /// <summary>Identifies whether the first anchor's X coordinate is fixed.</summary>
    public static readonly StyledProperty<bool> StartPointLockedXProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(StartPointLockedX), true);

    /// <summary>Identifies whether the first anchor's Y coordinate is fixed.</summary>
    public static readonly StyledProperty<bool> StartPointLockedYProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(StartPointLockedY));

    /// <summary>Identifies whether the last anchor's X coordinate is fixed.</summary>
    public static readonly StyledProperty<bool> EndPointLockedXProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(EndPointLockedX), true);

    /// <summary>Identifies whether the last anchor's Y coordinate is fixed.</summary>
    public static readonly StyledProperty<bool> EndPointLockedYProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(EndPointLockedY));

    /// <summary>Identifies whether horizontal bound changes scale anchor positions.</summary>
    public static readonly StyledProperty<bool> ScaleOnBoundChangeHorizontalProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(ScaleOnBoundChangeHorizontal), true);

    /// <summary>Identifies whether vertical bound changes scale anchor positions.</summary>
    public static readonly StyledProperty<bool> ScaleOnBoundChangeVerticalProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(ScaleOnBoundChangeVertical));

    /// <summary>Identifies whether the X coordinate snaps to snappable markers.</summary>
    public static readonly StyledProperty<bool> SnapXProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(SnapX));

    /// <summary>Identifies whether the Y coordinate snaps to snappable markers.</summary>
    public static readonly StyledProperty<bool> SnapYProperty =
        AvaloniaProperty.Register<GraphControl, bool>(nameof(SnapY));

    /// <summary>Identifies the graph-value range used for X marker snapping.</summary>
    public static readonly StyledProperty<double> MarkerSnappingRangeHorizontalProperty =
        AvaloniaProperty.Register<GraphControl, double>(nameof(MarkerSnappingRangeHorizontal), double.PositiveInfinity);

    /// <summary>Identifies the graph-value range used for Y marker snapping.</summary>
    public static readonly StyledProperty<double> MarkerSnappingRangeVerticalProperty =
        AvaloniaProperty.Register<GraphControl, double>(nameof(MarkerSnappingRangeVertical), double.PositiveInfinity);

    /// <summary>Identifies the brush used to fill the area under the curve.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(Fill));

    /// <summary>Identifies the brush used to draw the curve.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(Stroke));

    /// <summary>Identifies the brush used for graph edges.</summary>
    public static readonly StyledProperty<IBrush?> EdgeBrushProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(
            nameof(EdgeBrush),
            new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)));

    /// <summary>Identifies the translucent graph background brush.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(
            nameof(Background),
            new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)));

    /// <summary>Identifies the brush used for anchors.</summary>
    public static readonly StyledProperty<IBrush?> AnchorBrushProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(AnchorBrush));

    /// <summary>Identifies the brush used for anchor outlines.</summary>
    public static readonly StyledProperty<IBrush?> AnchorOutlineBrushProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(AnchorOutlineBrush));

    /// <summary>Identifies the brush used for tension handles.</summary>
    public static readonly StyledProperty<IBrush?> TensionBrushProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(TensionBrush));

    /// <summary>Identifies the brush used for axes and generated markers.</summary>
    public static readonly StyledProperty<IBrush?> MarkerBrushProperty =
        AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(MarkerBrush));

    private IPointer? capturedPointer;
    private bool committingState;
    private int? contextAnchorIndex;
    private ContextMenu? contextMenu;
    private bool drawAnchors;
    private int gestureAnchorIndex = -1;
    private Point gestureStartPosition;
    private double gestureStartTension;
    private Point lastPointerPosition;
    private bool viewInitialized;
    private double viewMaxX = 1;
    private double viewMaxY = 1;

    private double viewMinX;
    private double viewMinY;

    static GraphControl()
    {
        AffectsRender<GraphControl>(
            GraphStateProperty,
            MarkersProperty,
            HorizontalMarkerGeneratorProperty,
            VerticalMarkerGeneratorProperty,
            MinMarkerSpacingProperty,
            HorizontalAxisProperty,
            VerticalAxisProperty,
            HorizontalAxisVisibleProperty,
            VerticalAxisVisibleProperty,
            MarkerSnappingRangeHorizontalProperty,
            MarkerSnappingRangeVerticalProperty,
            IsEditableProperty,
            FillProperty,
            StrokeProperty,
            EdgeBrushProperty,
            BackgroundProperty,
            AnchorBrushProperty,
            AnchorOutlineBrushProperty,
            TensionBrushProperty,
            MarkerBrushProperty);

        GraphStateProperty.Changed.AddClassHandler<GraphControl>((control, _) => control.GraphStateChanged());
    }

    /// <summary>Creates a focusable, clipped graph editor.</summary>
    public GraphControl()
    {
        Focusable = true;
        IsTabStop = true;
    }

    /// <summary>Suppresses state-change notifications while a host batches anchor updates.</summary>
    public bool IgnoreAnchorUpdates { get; set; }

    /// <summary>Gets or sets the graph state edited by the control.</summary>
    public CoreGraphState? GraphState
    {
        get => GetValue(GraphStateProperty);
        set => SetValue(GraphStateProperty, value);
    }

    /// <summary>Gets or sets extra markers drawn over generated markers.</summary>
    public IReadOnlyList<GraphMarker> Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>Gets or sets the marker generator for graph X values.</summary>
    public IGraphMarkerGenerator? HorizontalMarkerGenerator
    {
        get => GetValue(HorizontalMarkerGeneratorProperty);
        set => SetValue(HorizontalMarkerGeneratorProperty, value);
    }

    /// <summary>Gets or sets the marker generator for graph Y values.</summary>
    public IGraphMarkerGenerator? VerticalMarkerGenerator
    {
        get => GetValue(VerticalMarkerGeneratorProperty);
        set => SetValue(VerticalMarkerGeneratorProperty, value);
    }

    /// <summary>Gets or sets the minimum generated-marker spacing in pixels.</summary>
    public double MinMarkerSpacing
    {
        get => GetValue(MinMarkerSpacingProperty);
        set => SetValue(MinMarkerSpacingProperty, value);
    }

    /// <summary>Gets or sets the graph X value represented by the horizontal axis.</summary>
    public double HorizontalAxis
    {
        get => GetValue(HorizontalAxisProperty);
        set => SetValue(HorizontalAxisProperty, value);
    }

    /// <summary>Gets or sets the graph Y value represented by the vertical axis.</summary>
    public double VerticalAxis
    {
        get => GetValue(VerticalAxisProperty);
        set => SetValue(VerticalAxisProperty, value);
    }

    /// <summary>Gets or sets whether the horizontal axis is visible.</summary>
    public bool HorizontalAxisVisible
    {
        get => GetValue(HorizontalAxisVisibleProperty);
        set => SetValue(HorizontalAxisVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the vertical axis is visible.</summary>
    public bool VerticalAxisVisible
    {
        get => GetValue(VerticalAxisVisibleProperty);
        set => SetValue(VerticalAxisVisibleProperty, value);
    }

    /// <summary>Gets or sets whether pointer editing and graph context menus are enabled.</summary>
    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    /// <summary>Gets or sets whether X movement snaps to generated or extra markers.</summary>
    public bool SnapX
    {
        get => GetValue(SnapXProperty);
        set => SetValue(SnapXProperty, value);
    }

    /// <summary>Gets or sets whether Y movement snaps to generated or extra markers.</summary>
    public bool SnapY
    {
        get => GetValue(SnapYProperty);
        set => SetValue(SnapYProperty, value);
    }

    /// <summary>Gets or sets the last interpolator used when a new anchor is inserted.</summary>
    public Type LastInterpolationSet
    {
        get => GetValue(LastInterpolationSetProperty);
        set => SetValue(LastInterpolationSetProperty, value ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <summary>Gets or sets whether the graph accepts pointer and context-menu edits.</summary>
    public bool UserEditable
    {
        get => IsEditable;
        set => IsEditable = value;
    }

    /// <summary>Gets or sets whether the first anchor's X coordinate is fixed at the minimum bound.</summary>
    public bool StartPointLockedX
    {
        get => GetValue(StartPointLockedXProperty);
        set => SetValue(StartPointLockedXProperty, value);
    }

    /// <summary>Gets or sets whether the first anchor's Y coordinate is fixed.</summary>
    public bool StartPointLockedY
    {
        get => GetValue(StartPointLockedYProperty);
        set => SetValue(StartPointLockedYProperty, value);
    }

    /// <summary>Gets or sets whether the last anchor's X coordinate is fixed at the maximum bound.</summary>
    public bool EndPointLockedX
    {
        get => GetValue(EndPointLockedXProperty);
        set => SetValue(EndPointLockedXProperty, value);
    }

    /// <summary>Gets or sets whether the last anchor's Y coordinate is fixed.</summary>
    public bool EndPointLockedY
    {
        get => GetValue(EndPointLockedYProperty);
        set => SetValue(EndPointLockedYProperty, value);
    }

    /// <summary>Gets or sets the legacy name for horizontal/X marker snapping.</summary>
    public bool MarkerSnappingHorizontal
    {
        get => SnapX;
        set => SnapX = value;
    }

    /// <summary>Gets or sets the legacy name for vertical/Y marker snapping.</summary>
    public bool MarkerSnappingVertical
    {
        get => SnapY;
        set => SnapY = value;
    }

    /// <summary>Gets or sets whether horizontal bound changes reset the horizontal view.</summary>
    public bool ScaleOnBoundChangeHorizontal
    {
        get => GetValue(ScaleOnBoundChangeHorizontalProperty);
        set => SetValue(ScaleOnBoundChangeHorizontalProperty, value);
    }

    /// <summary>Gets or sets whether vertical bound changes reset the vertical view.</summary>
    public bool ScaleOnBoundChangeVertical
    {
        get => GetValue(ScaleOnBoundChangeVerticalProperty);
        set => SetValue(ScaleOnBoundChangeVerticalProperty, value);
    }

    /// <summary>Gets or sets the maximum X distance at which a marker snaps an anchor.</summary>
    public double MarkerSnappingRangeHorizontal
    {
        get => GetValue(MarkerSnappingRangeHorizontalProperty);
        set => SetValue(MarkerSnappingRangeHorizontalProperty, value);
    }

    /// <summary>Gets or sets the maximum Y distance at which a marker snaps an anchor.</summary>
    public double MarkerSnappingRangeVertical
    {
        get => GetValue(MarkerSnappingRangeVerticalProperty);
        set => SetValue(MarkerSnappingRangeVerticalProperty, value);
    }

    /// <summary>Gets or sets extra markers drawn and considered for snapping.</summary>
    public IReadOnlyList<GraphMarker> ExtraMarkers
    {
        get => Markers;
        set => Markers = value;
    }

    /// <summary>Gets or sets the minimum graph X bound.</summary>
    public double MinX
    {
        get => GetGraphState().MinX;
        set => UpdateBounds(state => state.MinX = value);
    }

    /// <summary>Gets or sets the minimum graph Y bound.</summary>
    public double MinY
    {
        get => GetGraphState().MinY;
        set => UpdateBounds(state => state.MinY = value);
    }

    /// <summary>Gets or sets the maximum graph X bound.</summary>
    public double MaxX
    {
        get => GetGraphState().MaxX;
        set => UpdateBounds(state => state.MaxX = value);
    }

    /// <summary>Gets or sets the maximum graph Y bound.</summary>
    public double MaxY
    {
        get => GetGraphState().MaxY;
        set => UpdateBounds(state => state.MaxY = value);
    }

    /// <summary>Gets or sets the minimum visible graph X value.</summary>
    public double ViewMinX
    {
        get
        {
            EnsureView();
            return viewMinX;
        }
        set
        {
            viewMinX = value;
            viewInitialized = true;
            NormalizeView();
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the minimum visible graph Y value.</summary>
    public double ViewMinY
    {
        get
        {
            EnsureView();
            return viewMinY;
        }
        set
        {
            viewMinY = value;
            viewInitialized = true;
            NormalizeView();
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the maximum visible graph X value.</summary>
    public double ViewMaxX
    {
        get
        {
            EnsureView();
            return viewMaxX;
        }
        set
        {
            viewMaxX = value;
            viewInitialized = true;
            NormalizeView();
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the maximum visible graph Y value.</summary>
    public double ViewMaxY
    {
        get
        {
            EnsureView();
            return viewMaxY;
        }
        set
        {
            viewMaxY = value;
            viewInitialized = true;
            NormalizeView();
            InvalidateVisual();
        }
    }

    /// <summary>Gets the visible graph width.</summary>
    public double ViewWidth => ViewWidthInternal;

    /// <summary>Gets the visible graph height.</summary>
    public double ViewHeight => ViewHeightInternal;

    /// <summary>Gets or sets the curve fill brush.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Gets or sets the curve stroke brush.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>Gets or sets the graph edge brush.</summary>
    public IBrush? EdgeBrush
    {
        get => GetValue(EdgeBrushProperty);
        set => SetValue(EdgeBrushProperty, value);
    }

    /// <summary>Gets or sets the translucent brush drawn behind the graph.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>Gets or sets the anchor brush.</summary>
    public IBrush? AnchorBrush
    {
        get => GetValue(AnchorBrushProperty);
        set => SetValue(AnchorBrushProperty, value);
    }

    /// <summary>Gets or sets the anchor outline brush.</summary>
    public IBrush? AnchorOutlineBrush
    {
        get => GetValue(AnchorOutlineBrushProperty);
        set => SetValue(AnchorOutlineBrushProperty, value);
    }

    /// <summary>Gets or sets the tension-handle brush.</summary>
    public IBrush? TensionBrush
    {
        get => GetValue(TensionBrushProperty);
        set => SetValue(TensionBrushProperty, value);
    }

    /// <summary>Gets or sets the axis and marker brush.</summary>
    public IBrush? MarkerBrush
    {
        get => GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    /// <summary>Gets or sets the legacy graph-edge brush name.</summary>
    public IBrush? EdgesBrush
    {
        get => EdgeBrush;
        set => EdgeBrush = value;
    }

    /// <summary>Gets or sets the legacy anchor outline brush name.</summary>
    public IBrush? AnchorStroke
    {
        get => AnchorOutlineBrush;
        set => AnchorOutlineBrush = value;
    }

    /// <summary>Gets or sets the legacy anchor fill brush name.</summary>
    public IBrush? AnchorFill
    {
        get => AnchorBrush;
        set => AnchorBrush = value;
    }

    /// <summary>Gets or sets the legacy tension-handle outline brush name.</summary>
    public IBrush? TensionAnchorStroke
    {
        get => TensionBrush;
        set => TensionBrush = value;
    }

    /// <summary>Gets or sets the legacy tension-handle fill brush name.</summary>
    public IBrush? TensionAnchorFill
    {
        get => TensionBrush;
        set => TensionBrush = value;
    }

    /// <summary>Gets the currently selected anchor index, if any.</summary>
    public int? SelectedAnchorIndex { get; private set; }

    /// <summary>Gets the active pointer gesture.</summary>
    public GraphPointerGesture ActiveGesture { get; private set; }

    private double ViewWidthInternal => Math.Max(viewMaxX - viewMinX, MinimumViewSize);

    private double ViewHeightInternal => Math.Max(viewMaxY - viewMinY, MinimumViewSize);

    /// <summary>Raised after an edit produces a new cloned graph state.</summary>
    public event EventHandler<GraphStateChangedEventArgs>? StateChanged;

    /// <summary>Applies one brush to the curve, edges, anchors, and tension handles.</summary>
    /// <param name="brush">The brush to apply.</param>
    public void SetBrush(IBrush brush)
    {
        ArgumentNullException.ThrowIfNull(brush);
        IBrush transparentBrush = brush is SolidColorBrush solid
            ? new SolidColorBrush(solid.Color, 0.2)
            : Brushes.Transparent;
        Stroke = brush;
        Fill = transparentBrush;
        AnchorOutlineBrush = brush;
        AnchorBrush = transparentBrush;
        TensionBrush = brush;
    }

    /// <summary>Returns a deep editable copy of the current state, using the legacy default when empty.</summary>
    /// <returns>A graph state safe for independent editing.</returns>
    public CoreGraphState GetGraphState()
    {
        return GraphState?.Clone() ?? CoreGraphState.CreateDefault();
    }

    /// <summary>Resets the viewport to the graph bounds.</summary>
    public void ResetView()
    {
        var state = GetGraphState();
        viewMinX = state.MinX;
        viewMinY = state.MinY;
        viewMaxX = state.MaxX;
        viewMaxY = state.MaxY;
        NormalizeView();
        viewInitialized = true;
        InvalidateVisual();
    }

    /// <summary>Returns the graph-space value represented by a control point.</summary>
    /// <param name="point">The device-independent control point.</param>
    /// <returns>The corresponding graph-space coordinate.</returns>
    public Vector2 GetGraphPosition(Point point)
    {
        EnsureView();
        double x = viewMinX + point.X / Math.Max(Bounds.Width, 1) * ViewWidthInternal;
        double y = viewMaxY - point.Y / Math.Max(Bounds.Height, 1) * ViewHeightInternal;
        return new Vector2((float)x, (float)y);
    }

    /// <summary>Converts a control X coordinate into the visible graph X value.</summary>
    /// <param name="pointX">The device-independent control X coordinate.</param>
    /// <returns>The corresponding graph X value.</returns>
    public double GetValueX(double pointX)
    {
        EnsureView();
        return viewMinX + pointX / Math.Max(Bounds.Width, 1) * ViewWidthInternal;
    }

    /// <summary>Converts a control Y coordinate into the visible graph Y value.</summary>
    /// <param name="pointY">The device-independent control Y coordinate.</param>
    /// <returns>The corresponding graph Y value.</returns>
    public double GetValueY(double pointY)
    {
        EnsureView();
        return viewMinY + (Math.Max(Bounds.Height, 1) - pointY) / Math.Max(Bounds.Height, 1) * ViewHeightInternal;
    }

    /// <summary>Returns the control point represented by a graph-space position.</summary>
    /// <param name="position">The graph-space coordinate.</param>
    /// <returns>The corresponding device-independent control point.</returns>
    public Point GetControlPosition(Vector2 position)
    {
        EnsureView();
        double x = (position.X - viewMinX) / ViewWidthInternal * Bounds.Width;
        double y = (viewMaxY - position.Y) / ViewHeightInternal * Bounds.Height;
        return new Point(x, y);
    }

    /// <summary>Moves an anchor with the legacy lock, clamp, and snapping rules.</summary>
    /// <param name="anchorIndex">The anchor index to move.</param>
    /// <param name="position">The requested graph-space position.</param>
    /// <param name="modifiers">Keyboard modifiers active during the gesture.</param>
    /// <returns><see langword="true" /> when the state changed.</returns>
    public bool MoveAnchor(int anchorIndex, Vector2 position, KeyModifiers modifiers = KeyModifiers.None)
    {
        if (!IsEditable || GraphState is null || anchorIndex < 0 || anchorIndex >= GraphState.Anchors.Count) return false;

        var state = GraphState.Clone();
        var anchor = state.Anchors[anchorIndex];
        bool lockY = modifiers.HasAllFlags(KeyModifiers.Shift);
        bool lockX = modifiers.HasAllFlags(KeyModifiers.Control);
        if (lockY) position.Y = anchor.Pos.Y;
        if (lockX) position.X = anchor.Pos.X;

        if (!modifiers.HasAllFlags(KeyModifiers.Alt))
        {
            if (SnapX) position.X = Snap(position.X, GraphMarkerOrientation.Vertical);
            if (SnapY) position.Y = Snap(position.Y, GraphMarkerOrientation.Horizontal);
        }

        if (anchorIndex == 0 && StartPointLockedX)
            position.X = anchor.Pos.X;
        else if (anchorIndex == state.Anchors.Count - 1 && EndPointLockedX)
            position.X = anchor.Pos.X;
        else if (anchorIndex > 0 && anchorIndex < state.Anchors.Count - 1)
            position.X = Math.Clamp(
                position.X,
                state.Anchors[anchorIndex - 1].Pos.X,
                state.Anchors[anchorIndex + 1].Pos.X);

        position.Y = Math.Clamp(position.Y, state.MinY, state.MaxY);

        if (anchorIndex == 0 && StartPointLockedY)
            position.Y = anchor.Pos.Y;
        else if (anchorIndex == state.Anchors.Count - 1 && EndPointLockedY) position.Y = anchor.Pos.Y;

        if (Math.Abs(anchor.Pos.X - position.X) <= 1e-9 && Math.Abs(anchor.Pos.Y - position.Y) <= 1e-9) return false;

        anchor.Pos = position;
        CommitState(state);
        return true;
    }

    /// <summary>Resets a non-edge anchor's interpolation tension to zero.</summary>
    /// <param name="anchorIndex">The anchor whose tension should be reset.</param>
    /// <returns><see langword="true" /> when the state changed.</returns>
    public bool ResetTension(int anchorIndex)
    {
        if (!IsEditable || GraphState is null || anchorIndex <= 0 || anchorIndex >= GraphState.Anchors.Count) return false;

        var state = GraphState.Clone();
        if (Math.Abs(state.Anchors[anchorIndex].Tension) <= 1e-9) return false;

        state.Anchors[anchorIndex].Tension = 0;
        CommitState(state);
        return true;
    }

    /// <summary>Inserts an anchor into the graph and selects the new anchor.</summary>
    /// <param name="position">The graph-space position of the new anchor.</param>
    /// <returns>The inserted anchor index.</returns>
    public int AddAnchor(Vector2 position)
    {
        var state = GetGraphState();
        position.X = Math.Clamp(position.X, (float)state.MinX, (float)state.MaxX);
        position.Y = Math.Clamp(position.Y, (float)state.MinY, (float)state.MaxY);
        int index = state.Anchors.FindIndex(anchor => anchor.Pos.X >= position.X);
        if (index < 0)
            index = state.Anchors.Count == 0
                ? 0
                : Math.Min(Math.Max(state.Anchors.Count - 1, 1), state.Anchors.Count);

        var type = GraphInterpolatorCatalog.GetInterpolatorIndex(LastInterpolationSet) >= 0
            ? LastInterpolationSet
            : typeof(SingleCurveInterpolator);
        double tension = index < state.Anchors.Count ? state.Anchors[index].Tension : 0;
        state.Anchors.Insert(index, new GraphAnchor(position, GraphInterpolatorCatalog.GetInterpolator(type), tension));
        SelectedAnchorIndex = index;
        CommitState(state);
        return index;
    }

    /// <summary>Removes a non-edge anchor from the graph.</summary>
    /// <param name="anchorIndex">The anchor index to remove.</param>
    /// <returns><see langword="true" /> when an anchor was removed.</returns>
    public bool RemoveAnchor(int anchorIndex)
    {
        if (!IsEditable || GraphState is null || anchorIndex <= 0 || anchorIndex >= GraphState.Anchors.Count - 1) return false;

        var state = GraphState.Clone();
        state.Anchors.RemoveAt(anchorIndex);
        SelectedAnchorIndex = null;
        CommitState(state);
        return true;
    }

    /// <summary>Changes one anchor's interpolation type using the persisted catalog order.</summary>
    /// <param name="anchorIndex">The anchor index to update.</param>
    /// <param name="interpolatorType">The parameterless interpolator type.</param>
    /// <returns><see langword="true" /> when the type was changed.</returns>
    public bool SetInterpolator(int anchorIndex, Type interpolatorType)
    {
        if (!IsEditable || GraphState is null || anchorIndex <= 0 || anchorIndex >= GraphState.Anchors.Count) return false;

        var state = GraphState.Clone();
        var anchor = state.Anchors[anchorIndex];
        anchor.Interpolator = GraphInterpolatorCatalog.GetInterpolator(interpolatorType);
        LastInterpolationSet = interpolatorType;
        CommitState(state);
        return true;
    }

    /// <summary>Pans the viewport by a device-independent pixel delta.</summary>
    /// <param name="delta">The pointer delta in control coordinates.</param>
    public void PanBy(Vector2 delta)
    {
        EnsureView();
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        double dx = delta.X / Bounds.Width * ViewWidthInternal;
        double dy = -delta.Y / Bounds.Height * ViewHeightInternal;

        var state = GetGraphState();
        dx = ClampPanDelta(dx, viewMinX, viewMaxX, state.MinX, state.MaxX);
        dy = ClampPanDelta(dy, viewMinY, viewMaxY, state.MinY, state.MaxY);
        viewMinX += dx;
        viewMaxX += dx;
        viewMinY += dy;
        viewMaxY += dy;
        InvalidateVisual();
    }

    /// <summary>Zooms the viewport around a control point.</summary>
    /// <param name="point">The zoom focus point.</param>
    /// <param name="factor">The multiplicative zoom factor.</param>
    /// <param name="lockX">Whether the X axis should remain unchanged.</param>
    /// <param name="lockY">Whether the Y axis should remain unchanged.</param>
    public void ZoomAt(Point point, double factor, bool lockX = false, bool lockY = false)
    {
        EnsureView();
        if (!double.IsFinite(factor) || factor <= 0) return;
        var focus = GetGraphPosition(point);
        if (!lockX)
        {
            viewMinX = focus.X - (focus.X - viewMinX) / factor;
            viewMaxX = focus.X + (viewMaxX - focus.X) / factor;
        }

        if (!lockY)
        {
            viewMinY = focus.Y - (focus.Y - viewMinY) / factor;
            viewMaxY = focus.Y + (viewMaxY - focus.Y) / factor;
        }

        var state = GetGraphState();
        ConstrainView(ref viewMinX, ref viewMaxX, state.MinX, state.MaxX);
        ConstrainView(ref viewMinY, ref viewMaxY, state.MinY, state.MaxY);

        InvalidateVisual();
    }

    /// <summary>Rebuilds generated marker geometry and invalidates the control.</summary>
    public void RegenerateMarkers()
    {
        InvalidateVisual();
    }

    /// <summary>Returns a graph-space movement for a device-independent pointer delta.</summary>
    /// <param name="delta">The pointer delta in control coordinates.</param>
    /// <returns>The corresponding graph-space delta.</returns>
    public Vector2 GetValueVector(Point delta)
    {
        return new Vector2(
            delta.X / Math.Max(Bounds.Width, 1) * ViewWidthInternal,
            -delta.Y / Math.Max(Bounds.Height, 1) * ViewHeightInternal);
    }

    /// <summary>Gets whether an anchor index is one of the two graph edges.</summary>
    /// <param name="anchorIndex">The anchor index to inspect.</param>
    /// <returns><see langword="true" /> for the first or last anchor.</returns>
    public bool IsEdgeAnchor(int anchorIndex)
    {
        return GraphState is not null && (anchorIndex == 0 || anchorIndex == GraphState.Anchors.Count - 1);
    }

    /// <summary>Replaces the graph with an independent state snapshot.</summary>
    /// <param name="state">The graph state to display and edit.</param>
    public void SetGraphState(CoreGraphState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        SetCurrentValue(GraphStateProperty, state.Clone());
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        EnsureView();
        Rect bounds = new(0, 0, Bounds.Width, Bounds.Height);

        context.DrawRectangle(Background, null, bounds);

        DrawMarkers(context);
        DrawAxes(context);
        DrawCurve(context);
        if (drawAnchors && IsEditable) DrawAnchors(context);

        if (EdgeBrush is not null) context.DrawRectangle(null, new Pen(EdgeBrush, 2), bounds);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerEventArgs eventArgs)
    {
        base.OnPointerEntered(eventArgs);
        drawAnchors = true;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs eventArgs)
    {
        base.OnPointerExited(eventArgs);
        if (ActiveGesture == GraphPointerGesture.None)
        {
            drawAnchors = false;
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        Focus();
        var point = eventArgs.GetCurrentPoint(this).Position;
        var properties = eventArgs.GetCurrentPoint(this).Properties;

        if (properties.IsRightButtonPressed && IsEditable)
        {
            int? anchorIndex = HitTestAnchor(point);
            if (anchorIndex is int index)
            {
                SelectedAnchorIndex = index;
                if (eventArgs.KeyModifiers.HasAllFlags(KeyModifiers.Shift) && RemoveAnchor(index))
                {
                    eventArgs.Handled = true;
                    return;
                }

                OpenContextMenu(index);
                eventArgs.Handled = true;
                return;
            }

            int? rightTensionIndex = HitTestTension(point);
            if (rightTensionIndex is int resetIndex)
            {
                SelectedAnchorIndex = resetIndex;
                ResetTension(resetIndex);
                eventArgs.Handled = true;
                return;
            }

            int newIndex = AddAnchor(GetGraphPosition(point));
            BeginGesture(eventArgs.Pointer, GraphPointerGesture.Anchor, newIndex, point);
            eventArgs.Handled = true;
            return;
        }

        if (properties.IsMiddleButtonPressed)
        {
            BeginGesture(eventArgs.Pointer, GraphPointerGesture.Pan, -1, point);
            eventArgs.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed) return;

        int? hitAnchor = IsEditable ? HitTestAnchor(point) : null;
        if (hitAnchor is int anchor)
        {
            SelectedAnchorIndex = anchor;
            BeginGesture(eventArgs.Pointer, GraphPointerGesture.Anchor, anchor, point);
            eventArgs.Handled = true;
            return;
        }

        int? tensionAnchor = IsEditable ? HitTestTension(point) : null;
        if (tensionAnchor is int tensionIndex)
        {
            SelectedAnchorIndex = tensionIndex;
            var state = GetGraphState();
            gestureStartTension = state.Anchors[tensionIndex].Tension;
            gestureStartPosition = point;
            BeginGesture(eventArgs.Pointer, GraphPointerGesture.Tension, tensionIndex, point);
            eventArgs.Handled = true;
        }
        else
        {
            BeginGesture(eventArgs.Pointer, GraphPointerGesture.Pan, -1, point);
            eventArgs.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        var point = eventArgs.GetCurrentPoint(this).Position;
        if (capturedPointer != eventArgs.Pointer || ActiveGesture == GraphPointerGesture.None) return;

        switch (ActiveGesture)
        {
            case GraphPointerGesture.Anchor:
                MoveAnchor(gestureAnchorIndex, GetGraphPosition(point), eventArgs.KeyModifiers);
                break;
            case GraphPointerGesture.Tension:
                MoveTension(gestureAnchorIndex, point.Y, eventArgs.KeyModifiers);
                break;
            case GraphPointerGesture.Pan:
                PanBy(new Vector2((float)(lastPointerPosition.X - point.X), (float)(lastPointerPosition.Y - point.Y)));
                break;
        }

        lastPointerPosition = point;
        eventArgs.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs eventArgs)
    {
        base.OnPointerReleased(eventArgs);
        if (capturedPointer != eventArgs.Pointer) return;

        EndGesture(eventArgs.Pointer);
        eventArgs.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs eventArgs)
    {
        base.OnPointerCaptureLost(eventArgs);
        if (capturedPointer == eventArgs.Pointer) EndGesture(eventArgs.Pointer);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs eventArgs)
    {
        base.OnPointerWheelChanged(eventArgs);
        var state = GetGraphState();
        var zoomPoint = GetGraphPosition(eventArgs.GetPosition(this));
        if (!IsWheelZoomPositionInLegacyBounds(zoomPoint, state)) return;

        double factor = Math.Pow(2, -eventArgs.Delta.Y / 240d);
        if (double.IsFinite(factor) && factor > 0)
        {
            ZoomAt(
                eventArgs.GetPosition(this),
                factor,
                eventArgs.KeyModifiers.HasAllFlags(KeyModifiers.Control),
                eventArgs.KeyModifiers.HasAllFlags(KeyModifiers.Shift));
            eventArgs.Handled = true;
        }
    }

    internal static bool IsWheelZoomPositionInLegacyBounds(Vector2 zoomPoint, CoreGraphState state)
    {
        return zoomPoint.X >= 0 && zoomPoint.Y >= 0 && zoomPoint.X <= state.MaxX && zoomPoint.Y <= state.MaxY;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        base.OnKeyDown(eventArgs);
        if (eventArgs.Key == Key.Escape)
        {
            SelectedAnchorIndex = null;
            InvalidateVisual();
            eventArgs.Handled = true;
        }
        else if ((eventArgs.Key == Key.Delete || eventArgs.Key == Key.Back) && SelectedAnchorIndex is int index)
        {
            eventArgs.Handled = RemoveAnchor(index);
        }
    }

    private void GraphStateChanged()
    {
        if (!committingState)
        {
            viewInitialized = false;
            SelectedAnchorIndex = null;
        }

        EnsureView();
        InvalidateVisual();
    }

    private void EnsureView()
    {
        if (!viewInitialized) ResetView();
    }

    private void NormalizeView()
    {
        if (!double.IsFinite(viewMinX) || !double.IsFinite(viewMaxX) || viewMaxX - viewMinX < MinimumViewSize) viewMaxX = viewMinX + 1;

        if (!double.IsFinite(viewMinY) || !double.IsFinite(viewMaxY) || viewMaxY - viewMinY < MinimumViewSize) viewMaxY = viewMinY + 1;
    }

    private void BeginGesture(IPointer pointer, GraphPointerGesture nextGesture, int anchorIndex, Point point)
    {
        capturedPointer?.Capture(null);
        capturedPointer = pointer;
        ActiveGesture = nextGesture;
        gestureAnchorIndex = anchorIndex;
        gestureStartPosition = point;
        lastPointerPosition = point;
        pointer.Capture(this);
        drawAnchors = true;
        InvalidateVisual();
    }

    private void EndGesture(IPointer pointer)
    {
        pointer.Capture(null);
        capturedPointer = null;
        ActiveGesture = GraphPointerGesture.None;
        gestureAnchorIndex = -1;
        if (!IsPointerOver) drawAnchors = false;

        InvalidateVisual();
    }

    private void CommitState(CoreGraphState state)
    {
        committingState = true;
        try
        {
            SetCurrentValue(GraphStateProperty, state);
        }
        finally
        {
            committingState = false;
        }

        if (!IgnoreAnchorUpdates) StateChanged?.Invoke(this, new GraphStateChangedEventArgs(state.Clone()));
        InvalidateVisual();
    }

    private double Snap(double value, GraphMarkerOrientation orientation)
    {
        var markers = EnumerateMarkers(orientation).Where(marker => marker.Snappable);
        double configuredRange = orientation == GraphMarkerOrientation.Vertical
            ? MarkerSnappingRangeHorizontal
            : MarkerSnappingRangeVertical;
        double tolerance = double.IsFinite(configuredRange) ? configuredRange : double.PositiveInfinity;

        var closest = markers
            .OrderBy(marker => Math.Abs(marker.Value - value))
            .FirstOrDefault();
        return closest is not null && Math.Abs(closest.Value - value) <= tolerance ? closest.Value : value;
    }

    private void UpdateBounds(Action<CoreGraphState> update)
    {
        var state = GetGraphState();
        double oldMinX = state.MinX;
        double oldMaxX = state.MaxX;
        double oldMinY = state.MinY;
        double oldMaxY = state.MaxY;
        update(state);
        if (state.MaxX <= state.MinX) state.MaxX = state.MinX + 1;
        if (state.MaxY <= state.MinY) state.MaxY = state.MinY + 1;

        if (ScaleOnBoundChangeHorizontal || ScaleOnBoundChangeVertical)
            foreach (var anchor in state.Anchors)
            {
                double x = ScaleOnBoundChangeHorizontal && oldMaxX > oldMinX
                    ? state.MinX + (state.MaxX - state.MinX) * (anchor.Pos.X - oldMinX) / (oldMaxX - oldMinX)
                    : anchor.Pos.X;
                double y = ScaleOnBoundChangeVertical && oldMaxY > oldMinY
                    ? state.MinY + (state.MaxY - state.MinY) * (anchor.Pos.Y - oldMinY) / (oldMaxY - oldMinY)
                    : anchor.Pos.Y;
                anchor.Pos = new Vector2((float)x, (float)y);
            }

        viewInitialized = false;
        CommitState(state);
    }

    private static double ClampPanDelta(double delta, double viewMin, double viewMax, double boundMin, double boundMax)
    {
        double viewSize = viewMax - viewMin;
        double boundSize = boundMax - boundMin;
        if (viewSize >= boundSize) return boundMin - viewMin;

        return Math.Clamp(delta, boundMin - viewMin, boundMax - viewMax);
    }

    private static void ConstrainView(ref double viewMin, ref double viewMax, double boundMin, double boundMax)
    {
        if (viewMax - viewMin >= boundMax - boundMin)
        {
            viewMin = boundMin;
            viewMax = boundMax;
            return;
        }

        double delta = viewMin < boundMin
            ? boundMin - viewMin
            : viewMax > boundMax
                ? boundMax - viewMax
                : 0;
        viewMin += delta;
        viewMax += delta;
    }

    private IEnumerable<GraphMarker> EnumerateMarkers(GraphMarkerOrientation orientation)
    {
        foreach (var marker in Markers ?? Array.Empty<GraphMarker>())
            if (marker.Visible && marker.Orientation == orientation)
                yield return marker;

        double pixelLength = orientation == GraphMarkerOrientation.Vertical ? Bounds.Width : Bounds.Height;
        int count = Math.Max(1, (int)(pixelLength / Math.Max(MinMarkerSpacing, 1)));
        var generator = orientation == GraphMarkerOrientation.Vertical
            ? HorizontalMarkerGenerator
            : VerticalMarkerGenerator;
        if (generator is null) yield break;

        double start = orientation == GraphMarkerOrientation.Vertical ? viewMinX : viewMinY;
        double end = orientation == GraphMarkerOrientation.Vertical ? viewMaxX : viewMaxY;
        foreach (var marker in generator.GenerateMarkers(start, end, orientation, count))
            if (marker.Visible)
                yield return marker;
    }

    private void DrawMarkers(DrawingContext context)
    {
        foreach (var marker in EnumerateMarkers(GraphMarkerOrientation.Vertical))
        {
            if (marker.Value < viewMinX - Precision.DoubleEpsilon || marker.Value > viewMaxX + Precision.DoubleEpsilon) continue;

            var top = GetControlPosition(new Vector2((float)marker.Value, (float)viewMaxY));
            DrawMarker(context, marker, top, true);
        }

        foreach (var marker in EnumerateMarkers(GraphMarkerOrientation.Horizontal))
        {
            if (marker.Value < viewMinY - Precision.DoubleEpsilon || marker.Value > viewMaxY + Precision.DoubleEpsilon) continue;

            var left = GetControlPosition(new Vector2((float)viewMinX, (float)marker.Value));
            DrawMarker(context, marker, left, false);
        }
    }

    private void DrawMarker(DrawingContext context, GraphMarker marker, Point position, bool vertical)
    {
        var markerBrush = EdgeBrush ?? MarkerBrush;
        var lineBrush = marker.CustomLineColorArgb is uint customLineColor
            ? ToBrush(customLineColor)
            : markerBrush;
        if (lineBrush is null) return;

        var extensionBrush = marker.MarkerColorArgb is uint markerColor
            ? ToBrush(markerColor)
            : markerBrush ?? lineBrush;
        double length = marker.DrawMarker ? Math.Max(marker.MarkerLength, 0) : 0;
        var end = vertical
            ? new Point(position.X, position.Y + Math.Max(Bounds.Height, 0))
            : new Point(position.X + Math.Max(Bounds.Width, 0), position.Y);
        if (vertical)
        {
            context.DrawLine(new Pen(lineBrush), position, end);
            if (length > 0) context.DrawLine(new Pen(extensionBrush), end, new Point(end.X, end.Y + length));
        }
        else
        {
            context.DrawLine(new Pen(lineBrush), position, end);
            if (length > 0) context.DrawLine(new Pen(extensionBrush), position, new Point(position.X - length, position.Y));
        }

        if (!string.IsNullOrEmpty(marker.Text))
        {
            var textBrush = markerBrush ?? lineBrush;
            FormattedText text = new(
                marker.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                16,
                textBrush);
            context.DrawText(text, vertical
                ? new Point(position.X - text.Width / 2, end.Y + 5 + length)
                : new Point(position.X - 5 - text.Width - length, position.Y - text.Height / 2));
        }
    }

    private void DrawAxes(DrawingContext context)
    {
        if (EdgeBrush is null) return;
        if (HorizontalAxisVisible && HorizontalAxis >= viewMinY && HorizontalAxis <= viewMaxY)
        {
            var left = GetControlPosition(new Vector2((float)viewMinX, (float)HorizontalAxis));
            var right = GetControlPosition(new Vector2((float)viewMaxX, (float)HorizontalAxis));
            context.DrawLine(new Pen(EdgeBrush, 3), left, right);
        }

        if (VerticalAxisVisible && VerticalAxis >= viewMinX && VerticalAxis <= viewMaxX)
        {
            var top = GetControlPosition(new Vector2((float)VerticalAxis, (float)viewMaxY));
            var bottom = GetControlPosition(new Vector2((float)VerticalAxis, (float)viewMinY));
            context.DrawLine(new Pen(EdgeBrush, 3), top, bottom);
        }
    }

    private void DrawCurve(DrawingContext context)
    {
        var state = GetGraphState();
        if (state.Anchors.Count < 2) return;

        double start = Math.Max(viewMinX, state.Anchors[0].Pos.X);
        double end = Math.Min(viewMaxX, state.Anchors[^1].Pos.X);
        if (end < start) return;

        double width = Math.Max(end - start, 0) / ViewWidthInternal * Math.Max(Bounds.Width, 1);
        int samples = Math.Clamp((int)Math.Ceiling(Math.Max(width, 1)), 2, MaximumCurveSamples);
        List<Point> points = new(samples);
        for (int index = 0; index < samples; index++)
        {
            double x = start + (end - start) * index / (samples - 1);
            double y = Math.Clamp(state.GetValue(x), viewMinY, viewMaxY);
            points.Add(GetControlPosition(new Vector2((float)x, (float)y)));
        }

        if (points.Count == 0) return;
        if (Fill is not null && Fill != Brushes.Transparent)
        {
            StreamGeometry geometry = new();
            using (var geometryContext = geometry.Open())
            {
                double baseline = Math.Clamp(VerticalAxis, viewMinY, viewMaxY);
                geometryContext.BeginFigure(GetControlPosition(new Vector2((float)start, (float)baseline)));
                foreach (var point in points) geometryContext.LineTo(point);
                geometryContext.LineTo(GetControlPosition(new Vector2((float)end, (float)baseline)));
                geometryContext.EndFigure(true);
            }

            context.DrawGeometry(Fill, null, geometry);
        }

        if (Stroke is not null)
        {
            Pen pen = new(Stroke, 2);
            for (int index = 1; index < points.Count; index++) context.DrawLine(pen, points[index - 1], points[index]);
        }
    }

    private void DrawAnchors(DrawingContext context)
    {
        var state = GetGraphState();
        for (int index = 1; index < state.Anchors.Count; index++)
        {
            var previous = state.Anchors[index - 1];
            var anchor = state.Anchors[index];
            double midpoint = (previous.Pos.X + anchor.Pos.X) / 2;
            Vector2 tensionPosition = new((float)midpoint, (float)state.GetValue(midpoint));
            if (!IsGraphPositionVisible(tensionPosition)) continue;

            var curvePoint = GetControlPosition(tensionPosition);
            if (TensionBrush is not null) context.DrawEllipse(TensionBrush, AnchorOutlineBrush is null ? null : new Pen(AnchorOutlineBrush), curvePoint, 3.5, 3.5);
        }

        for (int index = 0; index < state.Anchors.Count; index++)
        {
            if (!IsGraphPositionVisible(state.Anchors[index].Pos)) continue;

            var point = GetControlPosition(state.Anchors[index].Pos);
            var outline = index == SelectedAnchorIndex ? Stroke : AnchorOutlineBrush;
            context.DrawEllipse(AnchorBrush, outline is null ? null : new Pen(outline, 2), point, 6, 6);
        }
    }

    private int? HitTestAnchor(Point point)
    {
        var state = GetGraphState();
        for (int index = 0; index < state.Anchors.Count; index++)
        {
            if (!IsGraphPositionVisible(state.Anchors[index].Pos)) continue;

            if (Distance(point, GetControlPosition(state.Anchors[index].Pos)) <= AnchorHitRadius) return index;
        }

        return null;
    }

    private int? HitTestTension(Point point)
    {
        var state = GetGraphState();
        for (int index = 1; index < state.Anchors.Count; index++)
        {
            double midpoint = (state.Anchors[index - 1].Pos.X + state.Anchors[index].Pos.X) / 2;
            Vector2 tensionPosition = new((float)midpoint, (float)state.GetValue(midpoint));
            if (!IsGraphPositionVisible(tensionPosition)) continue;

            var tensionPoint = GetControlPosition(tensionPosition);
            if (Distance(point, tensionPoint) <= TensionHitRadius) return index;
        }

        return null;
    }

    private void MoveTension(int anchorIndex, double pointerY, KeyModifiers modifiers)
    {
        if (!IsEditable || GraphState is null || anchorIndex <= 0 || anchorIndex >= GraphState.Anchors.Count) return;

        double verticalDrag = pointerY - gestureStartPosition.Y;
        if (modifiers.HasAllFlags(KeyModifiers.Control)) verticalDrag /= 10;
        var state = GraphState.Clone();
        var anchor = state.Anchors[anchorIndex];
        if (anchor.Interpolator.GetType().IsDefined(typeof(VerticalMirrorInterpolatorAttribute), false) && anchor.Pos.Y < state.Anchors[anchorIndex - 1].Pos.Y)
            verticalDrag = -verticalDrag;

        double tension = gestureStartTension - verticalDrag / 200;

        if (Math.Abs(anchor.Tension - tension) <= 1e-9) return;
        anchor.Tension = tension;
        CommitState(state);
    }

    private void OpenContextMenu(int anchorIndex)
    {
        contextAnchorIndex = anchorIndex;
        contextMenu ??= CreateContextMenu();
        UpdateContextMenu();
        contextMenu.Open(this);
    }

    private ContextMenu CreateContextMenu()
    {
        ContextMenu menu = new();
        MenuItem delete = new() { Header = "Delete" };
        delete.Click += (_, _) =>
        {
            if (contextAnchorIndex is int index) RemoveAnchor(index);
        };
        menu.Items.Add(delete);
        menu.Items.Add(new Separator());

        foreach (var type in GraphInterpolatorCatalog.GetInterpolators())
        {
            MenuItem item = new()
            {
                Header = GraphInterpolatorCatalog.GetName(type),
                Tag = type,
            };
            item.Click += (_, _) =>
            {
                if (contextAnchorIndex is int index && item.Tag is Type interpolatorType) SetInterpolator(index, interpolatorType);
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        MenuItem typeIn = new() { Header = "Type in value..." };
        typeIn.Click += (_, _) => TypeInValueAsync();
        menu.Items.Add(typeIn);
        return menu;
    }

    private void UpdateContextMenu()
    {
        if (contextMenu is null || contextAnchorIndex is not int index || GraphState is null) return;
        if (contextMenu.Items[0] is MenuItem delete) delete.IsEnabled = index > 0 && index < GraphState.Anchors.Count - 1;

        for (int itemIndex = 2; itemIndex < contextMenu.Items.Count; itemIndex++)
            if (contextMenu.Items[itemIndex] is MenuItem item && item.Tag is Type type)
            {
                item.IsEnabled = index > 0;
                item.IsChecked = GraphState.Anchors[index].Interpolator.GetType() == type;
            }
    }

    private async void TypeInValueAsync()
    {
        if (contextAnchorIndex is not int index || GraphState is null || TopLevel.GetTopLevel(this) is not Window owner) return;

        var anchor = GraphState.Anchors[index];
        ValueDialogWindow dialog = new();
        ValueDialogViewModel viewModel = new(
            "Graph value",
            "Value",
            anchor.Pos.Y,
            new InvariantDoubleConverter(),
            typeof(double),
            "OK",
            "CANCEL",
            _ => ValidationResult.Success,
            value =>
            {
                if (value is double number)
                {
                    var state = GraphState.Clone();
                    state.Anchors[index].Pos = new Vector2(state.Anchors[index].Pos.X, (float)number);
                    CommitState(state);
                }

                dialog.Close();
            },
            dialog.Close);
        dialog.DataContext = viewModel;
        await dialog.ShowDialog(owner);
    }

    private static double Distance(Point first, Point second)
    {
        double x = first.X - second.X;
        double y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private bool IsGraphPositionVisible(Vector2 position)
    {
        return position.X >= viewMinX - Precision.DoubleEpsilon
               && position.X <= viewMaxX + Precision.DoubleEpsilon
               && position.Y >= viewMinY - Precision.DoubleEpsilon
               && position.Y <= viewMaxY + Precision.DoubleEpsilon;
    }

    private static IBrush ToBrush(uint argb)
    {
        return new SolidColorBrush(Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb));
    }
}

internal static class GraphKeyModifiersExtensions
{
    public static bool HasAllFlags(this KeyModifiers value, KeyModifiers flags)
    {
        return (value & flags) == flags;
    }
}
