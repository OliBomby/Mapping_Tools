using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Reduces binding validation failures to a concise user-facing reason.</summary>
public sealed class ValidationErrorMessageConverter : IValueConverter
{
    /// <summary>
    /// Gets the Avalonia validation-pipeline adapter that replaces raw exceptions with their concise reason.
    /// </summary>
    public static Func<object, object> ConvertError { get; } = Message;

    /// <summary>Returns only the useful message from a validation error.</summary>
    /// <param name="value">An exception, binding notification, or validation message.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="parameter">Unused converter configuration.</param>
    /// <param name="culture">Unused display culture.</param>
    /// <returns>A single concise validation reason.</returns>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) => Message(value);

    private static object Message(object? value) => value switch
        {
            BindingNotification { Error: { } error } => Reason(error),
            Exception exception => Reason(exception),
            _ => FirstLine(value?.ToString())
        };

    /// <summary>Does not support converting display messages back to validation errors.</summary>
    /// <param name="value">The edited display value.</param>
    /// <param name="targetType">The requested source type.</param>
    /// <param name="parameter">Unused converter configuration.</param>
    /// <param name="culture">Unused display culture.</param>
    /// <returns>A binding sentinel that leaves the source unchanged.</returns>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) => BindingOperations.DoNothing;

    private static string FirstLine(string? value) =>
        value?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
        ?? string.Empty;

    private static string Reason(Exception exception)
    {
        string message = exception.GetBaseException().Message;
        const string formatMarker = "System.FormatException: ";
        int start = message.IndexOf(formatMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return FirstLine(message);
        }

        start += formatMarker.Length;
        int fallback = message.IndexOf(", Fallback:", start, StringComparison.Ordinal);
        int closing = message.IndexOf("}'", start, StringComparison.Ordinal);
        int lineEnd = message.IndexOfAny(['\r', '\n'], start);
        int end = new[] { fallback, closing, lineEnd }
            .Where(index => index >= 0)
            .DefaultIfEmpty(message.Length)
            .Min();
        return message[start..end].Trim();
    }
}
