using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Mapping_Tools.Desktop.Controls.Timeline;

/// <summary>Draws a compact, themeable timeline and invokes navigation for clicked markers.</summary>
public sealed class TimelineControl : Control
{
    private const double reserved_right = 110;
    private const double label_top = 1;
    private const double element_top = 14;
    private const double element_height = 52;
    private const double default_height = element_top + element_height;
    private const double line_y = 50;
    private const double hit_tolerance = 4;

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

    private Cursor? handCursor;

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

    private double TimelineWidth => Math.Max(0, Bounds.Width - reserved_right);

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = CreateScale();
        double width = TimelineWidth;
        double elementHeight = Math.Max(0, Bounds.Height - element_top);
        if (LineBrush is not null) context.DrawLine(new Pen(LineBrush, 2), new Point(0, line_y), new Point(width, line_y));

        if (TickBrush is not null)
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
                context.DrawText(text, new Point(x, label_top));
                DrawMarker(context, x, NeutralOuterBrush, NeutralInnerBrush, elementHeight);
            }

        using (context.PushClip(new Rect(0, 0, width, Bounds.Height)))
        {
            foreach (var marker in Markers ?? [])
            {
                double x = scale.ToUnit(marker.Time) * width;
                var (outer, inner) = GetBrushes(marker.Kind);
                DrawMarker(context, x, outer, inner, elementHeight);
            }
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? default_height : Math.Max(default_height, availableSize.Height));
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        var marker = MarkerAt(eventArgs.GetPosition(this).X);
        Cursor = marker is null
            ? null
            : handCursor ??= new Cursor(StandardCursorType.Hand);
        ToolTip.SetTip(this, marker is null ? null : FormatToolTip(marker));
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed || MarkerAt(eventArgs.GetPosition(this).X) is not { } marker)
            return;

        if (NavigateCommand?.CanExecute(marker.Time) == true)
        {
            NavigateCommand.Execute(marker.Time);
            eventArgs.Handled = true;
        }
    }

    internal TimelineMarker? MarkerAt(double x)
    {
        return x < 0 || x > TimelineWidth
            ? null
            : CreateScale().FindNearest(Markers ?? [], x, TimelineWidth, hit_tolerance);
    }

    internal static string FormatToolTip(TimelineMarker marker)
    {
        return TimelineScale.FormatMarker(marker.Time);
    }

    private TimelineScale CreateScale()
    {
        return double.IsFinite(StartTime) && double.IsFinite(EndTime)
            ? new TimelineScale(StartTime, EndTime)
            : new TimelineScale(0, 20);
    }

    private static void DrawMarker(
        DrawingContext context,
        double x,
        IBrush? outer,
        IBrush? inner,
        double height)
    {
        if (outer is not null)
            using (context.PushOpacity(0.3))
            {
                context.FillRectangle(outer, new Rect(x - 2.5, element_top, 5, height));
            }

        if (inner is not null) context.FillRectangle(inner, new Rect(x - 0.5, element_top, 1, height));
    }

    private (IBrush? Outer, IBrush? Inner) GetBrushes(TimelineMarkerKind kind)
    {
        return kind switch
        {
            TimelineMarkerKind.Added => (AddedOuterBrush, AddedInnerBrush),
            TimelineMarkerKind.Changed => (ChangedOuterBrush, ChangedInnerBrush),
            TimelineMarkerKind.Removed => (RemovedOuterBrush, RemovedInnerBrush),
            TimelineMarkerKind.Accent => (AccentOuterBrush, AccentInnerBrush),
            _ => (NeutralOuterBrush, NeutralInnerBrush),
        };
    }
}
