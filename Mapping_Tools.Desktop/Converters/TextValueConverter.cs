using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Adapts a UI-independent typed text converter to Avalonia's two-way binding
/// pipeline and reports malformed edits as data-validation errors.
/// </summary>
/// <typeparam name="T">The typed value edited by a text control.</typeparam>
public sealed class TextValueConverter<T> : IValueConverter
{
    private readonly ITextValueConverter<T> _converter;
    private readonly TextConversionState? _state;

    /// <summary>
    /// Creates an Avalonia adapter over an application's culture and format contract.
    /// </summary>
    /// <param name="converter">Formats typed values and parses user-entered text.</param>
    /// <param name="state">
    /// Optional per-field state used when a submit command must observe conversion failures.
    /// </param>
    public TextValueConverter(
        ITextValueConverter<T> converter,
        TextConversionState? state = null)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _state = state;
    }

    /// <inheritdoc/>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (targetType != typeof(string))
        {
            return TypeError(typeof(T), targetType);
        }

        if (value is T typedValue)
        {
            return _converter.Format(typedValue);
        }

        if (value is null && default(T) is null)
        {
            return _converter.Format((T)value!);
        }

        return TypeError(typeof(T), targetType);
    }

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if ((targetType != typeof(T) && targetType != typeof(object))
            || (value is not null && value is not string))
        {
            return TypeError(typeof(string), targetType);
        }

        if (_converter.TryConvert(
                value as string,
                out T? converted,
                out string? errorMessage))
        {
            _state?.SetError(null);
            return converted!;
        }

        string message = errorMessage ?? "The value could not be parsed.";
        _state?.SetError(message);
        return new BindingNotification(
            new FormatException(message),
            BindingErrorType.DataValidationError);
    }

    private static BindingNotification TypeError(
        Type sourceType,
        Type targetType) =>
        new(
            new InvalidCastException(
                $"Cannot convert {sourceType.Name} to {targetType.Name}."),
            BindingErrorType.Error);
}

/// <summary>
/// Tracks the conversion status of one editable field whose submit command
/// must remain disabled while its text cannot be converted.
/// </summary>
public sealed class TextConversionState
{
    /// <summary>
    /// Raised when the field enters or leaves a conversion-error state.
    /// </summary>
    public event EventHandler? ErrorChanged;

    /// <summary>
    /// Gets the current format correction, or <see langword="null"/> after a successful conversion.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the current field text failed conversion.
    /// </summary>
    public bool HasError => ErrorMessage is not null;

    internal void SetError(string? errorMessage)
    {
        if (ErrorMessage == errorMessage)
        {
            return;
        }

        ErrorMessage = errorMessage;
        ErrorChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Exposes the application's standard typed text formats as shared Avalonia converters.
/// </summary>
public static class DesktopValueConverters
{
    /// <summary>
    /// Gets the invariant signed 32-bit integer converter.
    /// </summary>
    public static IValueConverter InvariantInt32 { get; } =
        new TextValueConverter<int>(TextValueConverters.InvariantInt32);

    /// <summary>
    /// Gets the invariant round-trip double converter.
    /// </summary>
    public static IValueConverter InvariantDouble { get; } =
        new TextValueConverter<double>(TextValueConverters.InvariantDouble);

    /// <summary>
    /// Gets the invariant constant-format duration converter.
    /// </summary>
    public static IValueConverter ConstantTimeSpan { get; } =
        new TextValueConverter<TimeSpan>(TextValueConverters.ConstantTimeSpan);
}
