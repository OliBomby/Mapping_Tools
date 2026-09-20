using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Desktop.Converters;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Converters;

/// <summary>Converts Hitsound Studio sample gain to and from editable percentage text.</summary>
public sealed class HitsoundStudioSampleVolumeConverter : IValueConverter
{
    private const double invariant_volume = -0.01;
    private static readonly InvariantDoubleConverter doubleConverter = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double volume) return string.Empty;

        if (Math.Abs(volume - invariant_volume) < 1e-9) return "-1";

        return doubleConverter.Convert(volume * 100, targetType, parameter, culture);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        object converted = doubleConverter.ConvertBack(value, targetType, parameter, culture);
        if (converted is not double percentage) return converted;

        return Math.Abs(percentage + 1) < 1e-9
            ? invariant_volume
            : percentage / 100;
    }
}
