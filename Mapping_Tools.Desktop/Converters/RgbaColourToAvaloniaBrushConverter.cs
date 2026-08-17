using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts a Core colour into a brush for compact palette swatches.</summary>
public sealed class RgbaColourToAvaloniaBrushConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RgbaColour colour
            ? new SolidColorBrush(Color.FromArgb(colour.A, colour.R, colour.G, colour.B))
            : throw new InvalidCastException("Expected an RgbaColour value.");

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Palette swatches are read-only.");
}
