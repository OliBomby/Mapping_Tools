using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Mapping_Tools.Application.Timeline;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Draws a compact, themeable timeline and invokes navigation for double-clicked markers.</summary>
public sealed class TimelineControl : Control
{
    private const double LabelHeight = 16;
    private const double HitTolerance = 4;
    private Cursor? _handCursor;

    public static readonly StyledProperty<IReadOnlyList<TimelineMarker>> MarkersProperty =
        AvaloniaProperty.Register<TimelineControl, IReadOnlyList<TimelineMarker>>(
            nameof(Markers),
            []);
    public static readonly StyledProperty<double> StartTimeProperty =
        AvaloniaProperty.Register<TimelineControl, double>(nameof(StartTime));
    public static readonly StyledProperty<double> EndTimeProperty =
        AvaloniaProperty.Register<TimelineControl, double>(nameof(EndTime), 20);
    public static readonly StyledProperty<ICommand?> NavigateCommandProperty =
        AvaloniaProperty.Register<TimelineControl, ICommand?>(nameof(NavigateCommand));
    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(LineBrush));
    public static readonly StyledProperty<IBrush?> TickBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(TickBrush));
    public static readonly StyledProperty<IBrush?> NeutralOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(NeutralOuterBrush));
    public static readonly StyledProperty<IBrush?> NeutralInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(NeutralInnerBrush));
    public static readonly StyledProperty<IBrush?> AddedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AddedOuterBrush));
    public static readonly StyledProperty<IBrush?> AddedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AddedInnerBrush));
    public static readonly StyledProperty<IBrush?> ChangedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(ChangedOuterBrush));
    public static readonly StyledProperty<IBrush?> ChangedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(ChangedInnerBrush));
    public static readonly StyledProperty<IBrush?> RemovedOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(RemovedOuterBrush));
    public static readonly StyledProperty<IBrush?> RemovedInnerBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(RemovedInnerBrush));
    public static readonly StyledProperty<IBrush?> AccentOuterBrushProperty =
        AvaloniaProperty.Register<TimelineControl, IBrush?>(nameof(AccentOuterBrush));
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

    public IReadOnlyList<TimelineMarker> Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public double StartTime { get => GetValue(StartTimeProperty); set => SetValue(StartTimeProperty, value); }
    public double EndTime { get => GetValue(EndTimeProperty); set => SetValue(EndTimeProperty, value); }
    public ICommand? NavigateCommand { get => GetValue(NavigateCommandProperty); set => SetValue(NavigateCommandProperty, value); }
    public IBrush? LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public IBrush? TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    public IBrush? NeutralOuterBrush { get => GetValue(NeutralOuterBrushProperty); set => SetValue(NeutralOuterBrushProperty, value); }
    public IBrush? NeutralInnerBrush { get => GetValue(NeutralInnerBrushProperty); set => SetValue(NeutralInnerBrushProperty, value); }
    public IBrush? AddedOuterBrush { get => GetValue(AddedOuterBrushProperty); set => SetValue(AddedOuterBrushProperty, value); }
    public IBrush? AddedInnerBrush { get => GetValue(AddedInnerBrushProperty); set => SetValue(AddedInnerBrushProperty, value); }
    public IBrush? ChangedOuterBrush { get => GetValue(ChangedOuterBrushProperty); set => SetValue(ChangedOuterBrushProperty, value); }
    public IBrush? ChangedInnerBrush { get => GetValue(ChangedInnerBrushProperty); set => SetValue(ChangedInnerBrushProperty, value); }
    public IBrush? RemovedOuterBrush { get => GetValue(RemovedOuterBrushProperty); set => SetValue(RemovedOuterBrushProperty, value); }
    public IBrush? RemovedInnerBrush { get => GetValue(RemovedInnerBrushProperty); set => SetValue(RemovedInnerBrushProperty, value); }
    public IBrush? AccentOuterBrush { get => GetValue(AccentOuterBrushProperty); set => SetValue(AccentOuterBrushProperty, value); }
    public IBrush? AccentInnerBrush { get => GetValue(AccentInnerBrushProperty); set => SetValue(AccentInnerBrushProperty, value); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        TimelineScale scale = CreateScale();
        double width = Bounds.Width;
        double height = Bounds.Height;
        double lineY = LabelHeight + Math.Max(0, height - LabelHeight) / 2;
        if (LineBrush is not null)
        {
            context.DrawLine(new Pen(LineBrush, 2), new Point(0, lineY), new Point(width, lineY));
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
                    Typeface.Default,
                    10,
                    TickBrush);
                context.DrawText(text, new Point(Math.Clamp(x - text.Width / 2, 0, Math.Max(0, width - text.Width)), 0));
            }
        }

        using (context.PushClip(new Rect(0, LabelHeight, width, Math.Max(0, height - LabelHeight))))
        {
            foreach (TimelineMarker marker in Markers ?? [])
            {
                double x = scale.ToUnit(marker.Time) * width;
                (IBrush? outer, IBrush? inner) = GetBrushes(marker.Kind);
                if (outer is not null)
                {
                    using (context.PushOpacity(0.3))
                    {
                        context.FillRectangle(outer, new Rect(x - 2.5, LabelHeight, 5, height - LabelHeight));
                    }
                }
                if (inner is not null)
                {
                    context.FillRectangle(inner, new Rect(x - 0.5, LabelHeight, 1, height - LabelHeight));
                }
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width,
        double.IsInfinity(availableSize.Height) ? 80 : Math.Max(50, availableSize.Height));

    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        TimelineMarker? marker = MarkerAt(eventArgs.GetPosition(this).X);
        Cursor = marker is null
            ? null
            : _handCursor ??= new Cursor(StandardCursorType.Hand);
        ToolTip.SetTip(this, marker is null
            ? null
            : marker.Label ?? TimelineScale.FormatMarker(marker.Time));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        if (eventArgs.ClickCount < 2 ||
            !eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
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

    internal TimelineMarker? MarkerAt(double x) => CreateScale()
        .FindNearest(Markers ?? [], x, Bounds.Width, HitTolerance);

    private TimelineScale CreateScale() =>
        double.IsFinite(StartTime) && double.IsFinite(EndTime)
            ? new TimelineScale(StartTime, EndTime)
            : new TimelineScale(0, 20);

    private (IBrush? Outer, IBrush? Inner) GetBrushes(TimelineMarkerKind kind) => kind switch
    {
        TimelineMarkerKind.Added => (AddedOuterBrush, AddedInnerBrush),
        TimelineMarkerKind.Changed => (ChangedOuterBrush, ChangedInnerBrush),
        TimelineMarkerKind.Removed => (RemovedOuterBrush, RemovedInnerBrush),
        TimelineMarkerKind.Accent => (AccentOuterBrush, AccentInnerBrush),
        _ => (NeutralOuterBrush, NeutralInnerBrush)
    };

}
