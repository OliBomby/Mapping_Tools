using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationValueConverter = Mapping_Tools.Application.Interactions.Converters.IValueConverter;

namespace Mapping_Tools.Desktop.Converters;

internal sealed class ValueDialogConverter : IValueConverter
{
    private readonly ApplicationValueConverter _converter;
    private readonly TextConversionState _state;

    public ValueDialogConverter(
        ApplicationValueConverter converter,
        TextConversionState state)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(state);
        _converter = converter;
        _state = state;
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
            _state);
}
