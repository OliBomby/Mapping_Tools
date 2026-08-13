using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Mapping_Tools.Application.Timeline;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Draws a compact, themeable timeline and invokes navigation for clicked markers.</summary>
public sealed class TimelineControl : Control
{
    private const double ReservedRight = 110;
    private const double LabelTop = 1;
    private const double ElementTop = 14;
    private const double ElementHeight = 52;
    private const double LineY = 50;
    private const double HitTolerance = 4;
    private Cursor? _handCursor;

    /// <summary>Identifies the semantic marker collection drawn on the timeline.</summary>
    public static readonly StyledProperty<IReadOnlyList<TimelineMarker>> MarkersProperty =
        AvaloniaProperty.Register<TimelineControl, IReadOnlyList<TimelineMarker>>(
            nameof(Markers),
            []);
    /// <summary>Identifies the first visible timestamp in milliseconds.</summary>
    public static readonly StyledProperty<double> StartTimeProperty =
        AvaloniaProperty.Register<TimelineControl, double>(nameof(StartTime));
    /// <summary>Identifies the final visible timestamp in milliseconds.</summary>
    public static readonly StyledProperty<double> EndTimeProperty =
        AvaloniaProperty.Register<TimelineControl, double>(nameof(EndTime), 20);
    /// <summary>Identifies the command invoked when a marker is clicked.</summary>
    public static readonly StyledProperty<ICommand?> NavigateCommandProperty =
        AvaloniaProperty.Register<TimelineControl, ICommand?>(nameof(NavigateCommand));
    /// <summary>Identifies the horizontal timeline brush.</summary>
    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(LineBrush));
    /// <summary>Identifies the timeline-label brush.</summary>
    public static readonly StyledProperty<IBrush?> TickBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(TickBrush));
    /// <summary>Identifies the translucent outer brush for neutral markers.</summary>
    public static readonly StyledProperty<IBrush?> NeutralOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(NeutralOuterBrush));
    /// <summary>Identifies the solid inner brush for neutral markers.</summary>
    public static readonly StyledProperty<IBrush?> NeutralInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(NeutralInnerBrush));
    /// <summary>Identifies the translucent outer brush for added markers.</summary>
    public static readonly StyledProperty<IBrush?> AddedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AddedOuterBrush));
    /// <summary>Identifies the solid inner brush for added markers.</summary>
    public static readonly StyledProperty<IBrush?> AddedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AddedInnerBrush));
    /// <summary>Identifies the translucent outer brush for changed markers.</summary>
    public static readonly StyledProperty<IBrush?> ChangedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(ChangedOuterBrush));
    /// <summary>Identifies the solid inner brush for changed markers.</summary>
    public static readonly StyledProperty<IBrush?> ChangedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(ChangedInnerBrush));
    /// <summary>Identifies the translucent outer brush for removed markers.</summary>
    public static readonly StyledProperty<IBrush?> RemovedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(RemovedOuterBrush));
    /// <summary>Identifies the solid inner brush for removed markers.</summary>
    public static readonly StyledProperty<IBrush?> RemovedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(RemovedInnerBrush));
    /// <summary>Identifies the translucent outer brush for highlighted markers.</summary>
    public static readonly StyledProperty<IBrush?> AccentOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AccentOuterBrush));
    /// <summary>Identifies the solid inner brush for highlighted markers.</summary>
    public static readonly StyledProperty<IBrush?> AccentInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AccentInnerBrush));

    static TimelineControl()
    {
        AffectsRender<TimelineControl>(
            MarkersProperty,
            StartTimeProperty,
            EndTimeProperty,
            LineBrushProperty,
            TickBrushProperty,
            NeutralOuterBrushProperty,
            NeutralInnerBrushProperty,
            AddedOuterBrushProperty,
            AddedInnerBrushProperty,
            ChangedOuterBrushProperty,
            ChangedInnerBrushProperty,
            RemovedOuterBrushProperty,
            RemovedInnerBrushProperty,
            AccentOuterBrushProperty,
            AccentInnerBrushProperty);
    }

    /// <summary>Gets or sets the semantic markers drawn on the timeline.</summary>
    public IReadOnlyList<TimelineMarker> Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>Gets or sets the first visible timestamp in milliseconds.</summary>
    public double StartTime
    {
        get => GetValue(StartTimeProperty);
        set => SetValue(StartTimeProperty, value);
    }

    /// <summary>Gets or sets the final visible timestamp in milliseconds.</summary>
    public double EndTime
    {
        get => GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }

    /// <summary>Gets or sets the command invoked with a marker timestamp after a click.</summary>
    public ICommand? NavigateCommand
    {
        get => GetValue(NavigateCommandProperty);
        set => SetValue(NavigateCommandProperty, value);
    }

    /// <summary>Gets or sets the horizontal timeline brush.</summary>
    public IBrush? LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }

    /// <summary>Gets or sets the timeline-label brush.</summary>
    public IBrush? TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }

    /// <summary>Gets or sets the translucent outer brush for neutral markers.</summary>
    public IBrush? NeutralOuterBrush { get => GetValue(NeutralOuterBrushProperty); set => SetValue(NeutralOuterBrushProperty, value); }

    /// <summary>Gets or sets the solid inner brush for neutral markers.</summary>
    public IBrush? NeutralInnerBrush { get => GetValue(NeutralInnerBrushProperty); set => SetValue(NeutralInnerBrushProperty, value); }

    /// <summary>Gets or sets the translucent outer brush for added markers.</summary>
    public IBrush? AddedOuterBrush { get => GetValue(AddedOuterBrushProperty); set => SetValue(AddedOuterBrushProperty, value); }

    /// <summary>Gets or sets the solid inner brush for added markers.</summary>
    public IBrush? AddedInnerBrush { get => GetValue(AddedInnerBrushProperty); set => SetValue(AddedInnerBrushProperty, value); }

    /// <summary>Gets or sets the translucent outer brush for changed markers.</summary>
    public IBrush? ChangedOuterBrush { get => GetValue(ChangedOuterBrushProperty); set => SetValue(ChangedOuterBrushProperty, value); }

    /// <summary>Gets or sets the solid inner brush for changed markers.</summary>
    public IBrush? ChangedInnerBrush { get => GetValue(ChangedInnerBrushProperty); set => SetValue(ChangedInnerBrushProperty, value); }

    /// <summary>Gets or sets the translucent outer brush for removed markers.</summary>
    public IBrush? RemovedOuterBrush { get => GetValue(RemovedOuterBrushProperty); set => SetValue(RemovedOuterBrushProperty, value); }

    /// <summary>Gets or sets the solid inner brush for removed markers.</summary>
    public IBrush? RemovedInnerBrush { get => GetValue(RemovedInnerBrushProperty); set => SetValue(RemovedInnerBrushProperty, value); }

    /// <summary>Gets or sets the translucent outer brush for highlighted markers.</summary>
    public IBrush? AccentOuterBrush { get => GetValue(AccentOuterBrushProperty); set => SetValue(AccentOuterBrushProperty, value); }

    /// <summary>Gets or sets the solid inner brush for highlighted markers.</summary>
    public IBrush? AccentInnerBrush { get => GetValue(AccentInnerBrushProperty); set => SetValue(AccentInnerBrushProperty, value); }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        TimelineScale scale = CreateScale();
        double width = TimelineWidth;
        if (LineBrush is not null)
        {
            context.DrawLine(new Pen(LineBrush, 2), new Point(0, LineY), new Point(width, LineY));
        }

        if (TickBrush is not null)
        {
            foreach (double tick in scale.GetTicks())
            {
                double x = scale.ToUnit(tick) * width;
                FormattedText text = new(
                    TimelineScale.FormatTick(tick),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    10,
                    TickBrush);
                context.DrawText(text, new Point(x, LabelTop));
                DrawMarker(context, x, NeutralOuterBrush, NeutralInnerBrush, isScaleMark: true);
            }
        }

        using (context.PushClip(new Rect(0, 0, width, Bounds.Height)))
        {
            foreach (TimelineMarker marker in Markers ?? [])
            {
                double x = scale.ToUnit(marker.Time) * width;
                (IBrush? outer, IBrush? inner) = GetBrushes(marker.Kind);
                DrawMarker(context, x, outer, inner, isScaleMark: false);
            }
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width,
        double.IsInfinity(availableSize.Height) ? 100 : Math.Max(100, availableSize.Height));

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        TimelineMarker? marker = MarkerAt(eventArgs.GetPosition(this).X);
        Cursor = marker is null
            ? null
            : _handCursor ??= new Cursor(StandardCursorType.Hand);
        ToolTip.SetTip(this, marker is null ? null : FormatToolTip(marker));
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            MarkerAt(eventArgs.GetPosition(this).X) is not { } marker)
        {
            return;
        }

        if (NavigateCommand?.CanExecute(marker.Time) == true)
        {
            NavigateCommand.Execute(marker.Time);
            eventArgs.Handled = true;
        }
    }

    internal TimelineMarker? MarkerAt(double x) => x < 0 || x > TimelineWidth
        ? null
        : CreateScale().FindNearest(Markers ?? [], x, TimelineWidth, HitTolerance);

    internal static string FormatToolTip(TimelineMarker marker)
        => TimelineScale.FormatMarker(marker.Time);

    private TimelineScale CreateScale() =>
        double.IsFinite(StartTime) && double.IsFinite(EndTime)
            ? new TimelineScale(StartTime, EndTime)
            : new TimelineScale(0, 20);

    private double TimelineWidth => Math.Max(0, Bounds.Width - ReservedRight);

    private static void DrawMarker(
        DrawingContext context,
        double x,
        IBrush? outer,
        IBrush? inner,
        bool isScaleMark)
    {
        double outerLeft = isScaleMark ? x : x - 1;
        if (outer is not null)
        {
            using (context.PushOpacity(0.3))
            {
                context.FillRectangle(outer, new Rect(outerLeft, ElementTop, 5, ElementHeight));
            }
        }

        if (inner is not null)
        {
            context.FillRectangle(inner, new Rect(outerLeft + 2, ElementTop, 1, ElementHeight));
        }
    }

    private (IBrush? Outer, IBrush? Inner) GetBrushes(TimelineMarkerKind kind) => kind switch
    {
        TimelineMarkerKind.Added => (AddedOuterBrush, AddedInnerBrush),
        TimelineMarkerKind.Changed => (ChangedOuterBrush, ChangedInnerBrush),
        TimelineMarkerKind.Removed => (RemovedOuterBrush, RemovedInnerBrush),
        TimelineMarkerKind.Accent => (AccentOuterBrush, AccentInnerBrush),
        _ => (NeutralOuterBrush, NeutralInnerBrush)
    };

}
