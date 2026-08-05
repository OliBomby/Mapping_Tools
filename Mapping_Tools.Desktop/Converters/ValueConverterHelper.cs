using System.Globalization;
using Avalonia.Data;
using ApplicationValueConverter = Mapping_Tools.Application.Interactions.Converters.IValueConverter;

namespace Mapping_Tools.Desktop.Converters;

internal static class ValueConverterHelper
{
    public static object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture,
        ApplicationValueConverter converter)
    {
        try
        {
            return converter.Convert(value, targetType, parameter, culture)!;
        }
        catch (InvalidCastException exception)
        {
            return new BindingNotification(exception, BindingErrorType.Error);
        }
    }

    public static object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture,
        ApplicationValueConverter converter,
        TextConversionState? state = null)
    {
        try
        {
            object? converted = converter.ConvertBack(
                value,
                targetType,
                parameter,
                culture);
            state?.SetError(null);
            return converted!;
        }
        catch (FormatException exception)
        {
            state?.SetError(exception.Message);
            return new BindingNotification(
                exception,
                BindingErrorType.DataValidationError);
        }
        catch (InvalidCastException exception)
        {
            return new BindingNotification(exception, BindingErrorType.Error);
        }
    }

    public static BindingNotification TypeError(
        Type sourceType,
        Type targetType) =>
        new(
            new InvalidCastException(
                $"Cannot convert {sourceType.Name} to {targetType.Name}."),
            BindingErrorType.Error);
}
