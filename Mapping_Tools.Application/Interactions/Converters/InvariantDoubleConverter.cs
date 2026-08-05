using System.Globalization;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Application.Interactions.Converters;

/// <summary>
/// Formats doubles invariantly and evaluates user-entered arithmetic expressions when converting back.
/// </summary>
public sealed class InvariantDoubleConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        ValueConverterHelper.RequireValue<double>(value, targetType)
            .ToString("R", CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        ValueConverterHelper.RequireTarget<double>(targetType);
        string text = ValueConverterHelper.RequireText(value, targetType);
        if (TypeConverters.TryParseDouble(text, out double converted))
        {
            return converted;
        }

        throw new FormatException("Enter a valid number or arithmetic expression.");
    }
}
