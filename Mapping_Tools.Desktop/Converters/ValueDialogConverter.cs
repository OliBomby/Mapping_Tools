using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationValueConverter = Mapping_Tools.Application.Interactions.Converters.IValueConverter;

namespace Mapping_Tools.Desktop.Converters;

internal sealed class ValueDialogConverter : IValueConverter
{
    private readonly ApplicationValueConverter _converter;
    private readonly Action<string?> _reportConversionError;

    public ValueDialogConverter(
        ApplicationValueConverter converter,
        Action<string?> reportConversionError)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(reportConversionError);
        _converter = converter;
        _reportConversionError = reportConversionError;
    }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        ValueConverterHelper.Convert(
            value,
            targetType,
            parameter,
            culture,
            _converter);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        ValueConverterHelper.ConvertBack(
            value,
            targetType,
            parameter,
            culture,
            _converter,
            _reportConversionError);
}
