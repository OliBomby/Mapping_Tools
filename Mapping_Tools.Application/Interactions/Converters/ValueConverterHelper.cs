namespace Mapping_Tools.Application.Interactions.Converters;

internal static class ValueConverterHelper
{
    public static T RequireValue<T>(object? value, Type targetType)
    {
        if (targetType != typeof(string))
        {
            throw TypeError(typeof(T), targetType);
        }

        return value is T typedValue
            ? typedValue
            : throw TypeError(value?.GetType() ?? typeof(object), typeof(T));
    }

    public static string RequireText(object? value, Type targetType)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        throw TypeError(value?.GetType() ?? typeof(object), targetType);
    }

    public static void RequireTarget<T>(Type targetType)
    {
        if (targetType != typeof(T) && targetType != typeof(object))
        {
            throw TypeError(typeof(string), targetType);
        }
    }

    public static InvalidCastException TypeError(
        Type sourceType,
        Type targetType) =>
        new($"Cannot convert {sourceType.Name} to {targetType.Name}.");
}
