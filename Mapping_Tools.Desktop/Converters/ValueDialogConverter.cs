using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

internal sealed class ValueDialogConverter : IValueConverter
{
    private readonly Func<object?, Type, object> _convert;
    private readonly Func<object?, Type, object> _convertBack;

    private ValueDialogConverter(
        Func<object?, Type, object> convert,
        Func<object?, Type, object> convertBack)
    {
        _convert = convert;
        _convertBack = convertBack;
    }

    public static ValueDialogConverter Create<T>(
        ITextValueConverter<T> converter,
        TextConversionState state)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(state);
        return new ValueDialogConverter(
            (value, targetType) => TextValueConverterHelper.Convert(
                value,
                targetType,
                converter),
            (value, targetType) => TextValueConverterHelper.ConvertBack(
                value,
                targetType,
                converter,
                state));
    }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        _convert(value, targetType);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        _convertBack(value, targetType);
}
