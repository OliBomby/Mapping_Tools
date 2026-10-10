using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Converters;

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
        return ValueConverterHelper.Convert(() =>
        {
            if (targetType != typeof(string))
                throw new InvalidCastException($"Cannot convert {typeof(string).Name} to {targetType.Name}.");

            return value switch
            {
                null => string.Empty,
                string text => text,
                _ => throw new InvalidCastException(
                    $"Cannot convert {value.GetType().Name} to {typeof(string).Name}."),
            };
        });
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.ConvertBack(() =>
        {
            ValueConverterHelper.RequireTarget<string>(targetType);
            return value switch
            {
                null => string.Empty,
                string text => text,
                _ => throw new InvalidCastException(
                    $"Cannot convert {value.GetType().Name} to {targetType.Name}."),
            };
        });
    }
}
