using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Converts osu! Circle Size values to the diameter used by the object visualizer.
/// </summary>
public sealed class CircleSizeToThicknessConverter : IValueConverter
{
    /// <summary>
    /// Converts a finite Circle Size value to the corresponding hit-object diameter.
    /// </summary>
    /// <param name="value">The Circle Size value supplied by the binding.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="parameter">An optional converter parameter, which is ignored.</param>
    /// <param name="culture">The culture supplied by Avalonia, which is ignored.</param>
    /// <returns>The hit-object diameter, or the legacy 50-pixel fallback for invalid input.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double circleSize && double.IsFinite(circleSize)
            ? Beatmap.GetHitObjectRadius(circleSize) * 2
            : 50d;
    }

    /// <summary>
    /// Rejects reverse conversion because the visualizer thickness is derived state.
    /// </summary>
    /// <param name="value">The visualizer thickness.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="parameter">An optional converter parameter, which is ignored.</param>
    /// <param name="culture">The culture supplied by Avalonia, which is ignored.</param>
    /// <returns>This converter never performs reverse conversion.</returns>
    /// <exception cref="NotSupportedException">Always thrown because the binding is one-way.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("CircleSizeToThicknessConverter is a one-way converter.");
    }
}
