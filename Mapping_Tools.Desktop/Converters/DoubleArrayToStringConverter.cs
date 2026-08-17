using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Formats comma-separated double values for Property Transformer filters.
/// </summary>
public sealed class DoubleArrayToStringConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        value is IEnumerable<double> values
            ? string.Join(", ", values.Select(item => item.ToString("R", CultureInfo.InvariantCulture)))
            : string.Empty;

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<double>();
        }

        string[] parts = text.Split(',');
        double[] values = new double[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            if (!TypeConverters.TryParseDouble(parts[index], out values[index]))
            {
                return new BindingNotification(
                    new FormatException("Enter comma-separated numbers."),
                    BindingErrorType.DataValidationError);
            }
        }

        return values;
    }
}
