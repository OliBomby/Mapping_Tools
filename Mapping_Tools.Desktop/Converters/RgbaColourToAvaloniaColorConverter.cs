using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts the core colour value used by osu! files to Avalonia's picker colour.
/// </summary>
public sealed class RgbaColourToAvaloniaColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is RgbaColour colour
            ? Color.FromArgb(colour.A, colour.R, colour.G, colour.B)
            : throw new InvalidCastException("Expected an RgbaColour value.");
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is Color colour
            ? RgbaColour.FromArgb(colour.A, colour.R, colour.G, colour.B)
            : throw new InvalidCastException("Expected an Avalonia Color value.");
    }
}
