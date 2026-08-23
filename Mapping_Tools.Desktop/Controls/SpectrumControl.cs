using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Renders a reusable spectrum frame without owning audio or decoder resources.</summary>
public sealed class SpectrumControl : Control
{
    private const double DefaultHeight = 64;

    /// <summary>Identifies the immutable spectrum frame drawn by the control.</summary>
    public static readonly StyledProperty<SpectrumFrame?> FrameProperty =
        AvaloniaProperty.Register<SpectrumControl, SpectrumFrame?>(nameof(Frame));

    /// <summary>Identifies the brush used for spectrum bars.</summary>
    public static readonly StyledProperty<IBrush?> BarBrushProperty =
        AvaloniaProperty.Register<SpectrumControl, IBrush?>(nameof(BarBrush));

    /// <summary>Identifies the optional background brush.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<SpectrumControl, IBrush?>(nameof(BackgroundBrush));

    /// <summary>Identifies the minimum horizontal bar width in pixels.</summary>
    public static readonly StyledProperty<double> MinimumBarWidthProperty =
        AvaloniaProperty.Register<SpectrumControl, double>(nameof(MinimumBarWidth), 1);

    /// <summary>Identifies the vertical magnitude multiplier.</summary>
    public static readonly StyledProperty<double> VerticalScaleProperty =
        AvaloniaProperty.Register<SpectrumControl, double>(nameof(VerticalScale), 1);

    static SpectrumControl()
    {
        AffectsRender<SpectrumControl>(
            FrameProperty,
            BarBrushProperty,
            BackgroundBrushProperty,
            MinimumBarWidthProperty,
            VerticalScaleProperty);
    }

    /// <summary>Gets or sets the immutable frame displayed by the control.</summary>
    public SpectrumFrame? Frame
    {
        get => GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    /// <summary>Gets or sets the brush used to fill non-empty bars.</summary>
    public IBrush? BarBrush
    {
        get => GetValue(BarBrushProperty);
        set => SetValue(BarBrushProperty, value);
    }

    /// <summary>Gets or sets the optional background brush.</summary>
    public IBrush? BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    /// <summary>Gets or sets the minimum bar width in pixels.</summary>
    public double MinimumBarWidth
    {
        get => GetValue(MinimumBarWidthProperty);
        set => SetValue(MinimumBarWidthProperty, value);
    }

    /// <summary>Gets or sets the multiplier applied to normalized magnitudes.</summary>
    public double VerticalScale
    {
        get => GetValue(VerticalScaleProperty);
        set => SetValue(VerticalScaleProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (BackgroundBrush is not null) context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

        if (BarBrush is null) return;

        foreach (var bar in CalculateBarRects(Frame, Bounds.Size, VerticalScale, MinimumBarWidth)) context.FillRectangle(BarBrush, bar);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width) ? 100 : Math.Max(0, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? DefaultHeight : Math.Max(0, availableSize.Height));
    }

    /// <summary>
    ///     Calculates the pixel rectangles used by the renderer for a frame and viewport.
    /// </summary>
    /// <param name="frame">The spectrum frame, or <see langword="null" /> for the empty state.</param>
    /// <param name="size">The available viewport size.</param>
    /// <param name="verticalScale">The positive vertical multiplier.</param>
    /// <param name="minimumBarWidth">The minimum bar width in pixels.</param>
    /// <returns>Bottom-aligned bar rectangles in frequency order.</returns>
    internal static IReadOnlyList<Rect> CalculateBarRects(
        SpectrumFrame? frame,
        Size size,
        double verticalScale,
        double minimumBarWidth)
    {
        if (frame is null
            || frame.IsEmpty
            || size.Width <= 0
            || size.Height <= 0
            || !double.IsFinite(verticalScale)
            || verticalScale <= 0
            || !double.IsFinite(minimumBarWidth)
            || minimumBarWidth <= 0
            || frame.PeakMagnitude <= 0)
            return [];

        double possibleBarCount = Math.Floor(size.Width / minimumBarWidth);
        int maximumBarCount = possibleBarCount >= int.MaxValue
            ? int.MaxValue
            : Math.Max(1, (int)possibleBarCount);
        int barCount = Math.Min(frame.Magnitudes.Count, maximumBarCount);
        double width = size.Width / barCount;
        var bars = new List<Rect>(barCount);
        for (int index = 0; index < barCount; index++)
        {
            int firstBin = index * frame.Magnitudes.Count / barCount;
            int exclusiveLastBin = (index + 1) * frame.Magnitudes.Count / barCount;
            double magnitude = 0;
            for (int bin = firstBin; bin < exclusiveLastBin; bin++) magnitude = Math.Max(magnitude, frame.Magnitudes[bin]);

            double normalized = Math.Clamp(magnitude / frame.PeakMagnitude * verticalScale, 0, 1);
            double height = normalized * size.Height;
            if (height <= 0) continue;

            double x = index * width;
            bars.Add(new Rect(x, size.Height - height, width, height));
        }

        return bars;
    }
}
