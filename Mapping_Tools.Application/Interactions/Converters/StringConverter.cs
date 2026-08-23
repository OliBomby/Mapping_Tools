using System.Globalization;

namespace Mapping_Tools.Application.Interactions.Converters;

/// <summary>
///     Preserves string values exactly while treating a missing value as an empty editable field.
/// </summary>
public sealed class StringConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (targetType != typeof(string)) throw ValueConverterHelper.TypeError(typeof(string), targetType);

        return value switch
        {
            null => string.Empty,
            string text => text,
            _ => throw ValueConverterHelper.TypeError(value.GetType(), typeof(string)),
        };
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        ValueConverterHelper.RequireTarget<string>(targetType);
        return value switch
        {
            null => string.Empty,
            string text => text,
            _ => throw ValueConverterHelper.TypeError(value.GetType(), targetType),
        };
    }
}
