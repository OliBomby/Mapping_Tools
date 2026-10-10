using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Tools.TumourGenerator.Templates;
using Material.Icons;

namespace Mapping_Tools.Desktop.Tools.TumourGenerator.Converters;

/// <summary>Maps tumour templates to the legacy compact list icons.</summary>
public sealed class TumourGeneratorTemplateIconConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            TumourTemplate.Square => MaterialIconKind.SquareOutline,
            TumourTemplate.Circle => MaterialIconKind.CircleOutline,
            TumourTemplate.Parabola => MaterialIconKind.Multiply,
            _ => MaterialIconKind.TriangleOutline,
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
