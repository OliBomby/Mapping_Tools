using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationValueConverter = Mapping_Tools.Application.Interactions.Converters.IValueConverter;

namespace Mapping_Tools.Desktop.Converters;

internal sealed class ValueDialogConverter : IValueConverter
{
    private readonly ApplicationValueConverter converter;
    private readonly Action<string?> reportConversionError;

    public ValueDialogConverter(
        ApplicationValueConverter converter,
        Action<string?> reportConversionError)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(reportConversionError);
        this.converter = converter;
        this.reportConversionError = reportConversionError;
    }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.Convert(
            value,
            targetType,
            parameter,
            culture,
            converter);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.ConvertBack(
            value,
            targetType,
            parameter,
            culture,
            converter,
            reportConversionError);
    }
}
