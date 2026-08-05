using Avalonia.Data;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

internal static class TextValueConverterHelper
{
    public static object Convert<T>(
        object? value,
        Type targetType,
        ITextValueConverter<T> converter)
    {
        if (targetType != typeof(string))
        {
            return TypeError(typeof(T), targetType);
        }

        if (value is T typedValue)
        {
            return converter.Format(typedValue);
        }

        if (value is null && default(T) is null)
        {
            return converter.Format((T)value!);
        }

        return TypeError(typeof(T), targetType);
    }

    public static object ConvertBack<T>(
        object? value,
        Type targetType,
        ITextValueConverter<T> converter,
        TextConversionState? state = null)
    {
        if ((targetType != typeof(T) && targetType != typeof(object))
            || (value is not null && value is not string))
        {
            return TypeError(typeof(string), targetType);
        }

        if (converter.TryConvert(
                value as string,
                out T? converted,
                out string? errorMessage))
        {
            state?.SetError(null);
            return converted!;
        }

        string message = errorMessage ?? "The value could not be parsed.";
        state?.SetError(message);
        return new BindingNotification(
            new FormatException(message),
            BindingErrorType.DataValidationError);
    }

    public static BindingNotification TypeError(
        Type sourceType,
        Type targetType) =>
        new(
            new InvalidCastException(
                $"Cannot convert {sourceType.Name} to {targetType.Name}."),
            BindingErrorType.Error);
}
