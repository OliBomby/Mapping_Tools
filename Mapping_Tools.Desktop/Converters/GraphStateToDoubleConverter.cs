using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Classes.Graph;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Bridges the legacy scalar slider surface to the shared graph state.</summary>
public sealed class GraphStateToDoubleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is GraphState state ? state.GetValue(0) : 0d;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number && double.IsFinite(number)) return GraphStateTextCodec.CreateConstant(number);

        return GraphStateTextCodec.CreateConstant(0);
    }
}
