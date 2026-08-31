using Avalonia.Data;

namespace Mapping_Tools.Desktop.Converters;

internal static class ValueConverterHelper
{
    public static T RequireValue<T>(object? value, Type targetType)
    {
        if (targetType != typeof(string)) throw TypeErrorException(typeof(T), targetType);

        return value is T typedValue
            ? typedValue
            : throw TypeErrorException(value?.GetType() ?? typeof(object), typeof(T));
    }

    public static string RequireText(object? value, Type targetType)
    {
        if (value is null) return string.Empty;

        if (value is string text) return text;

        throw TypeErrorException(value.GetType(), targetType);
    }

    public static void RequireTarget<T>(Type targetType)
    {
        if (targetType != typeof(T) && targetType != typeof(object))
            throw TypeErrorException(typeof(string), targetType);
    }

    public static object Convert(Func<object?> conversion)
    {
        try
        {
            return conversion()!;
        }
        catch (InvalidCastException exception)
        {
            return new BindingNotification(exception, BindingErrorType.Error);
        }
    }

    public static object ConvertBack(Func<object?> conversion)
    {
        try
        {
            return conversion()!;
        }
        catch (FormatException exception)
        {
            return new BindingNotification(exception, BindingErrorType.DataValidationError);
        }
        catch (InvalidCastException exception)
        {
            return new BindingNotification(exception, BindingErrorType.Error);
        }
    }

    public static BindingNotification TypeError(
        Type sourceType,
        Type targetType)
    {
        return new BindingNotification(
            new InvalidCastException(
                $"Cannot convert {sourceType.Name} to {targetType.Name}."),
            BindingErrorType.Error);
    }

    private static InvalidCastException TypeErrorException(
        Type sourceType,
        Type targetType)
    {
        return new InvalidCastException(
            $"Cannot convert {sourceType.Name} to {targetType.Name}.");
    }
}
