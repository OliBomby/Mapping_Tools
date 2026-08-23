using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Graph;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Adapts the legacy scalar-or-anchor graph text format to Avalonia binding.</summary>
public sealed class GraphStateTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return GraphStateTextCodec.Format(value as GraphState);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
            return new BindingNotification(
                new FormatException("A graph value must be text."),
                BindingErrorType.DataValidationError);

        return GraphStateTextCodec.TryParse(text, out var state)
            ? state
            : new BindingNotification(
                new FormatException("Enter a number or at least two graph anchors."),
                BindingErrorType.DataValidationError);
    }
}
