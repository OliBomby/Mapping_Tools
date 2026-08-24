using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mapping_Tools.Application.Interactions.Converters;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
///     Defines a modal field that parses and validates a typed value before acceptance.
/// </summary>
/// <typeparam name="TValue">The parsed value returned to the caller.</typeparam>
public sealed class ValueDialogRequest<TValue>
{
    /// <summary>
    ///     Creates a typed field request using UI-independent conversion and validation contracts.
    /// </summary>
    /// <param name="title">The owner-window title-bar text.</param>
    /// <param name="prompt">The label or instruction placed above the field.</param>
    /// <param name="initialValue">The value formatted into the field when it opens.</param>
    /// <param name="converter">The bidirectional text representation.</param>
    /// <param name="validators">DataAnnotations rules evaluated in order after successful parsing.</param>
    /// <param name="acceptLabel">The default-action button text.</param>
    /// <param name="cancelLabel">The Escape-action button text.</param>
    public ValueDialogRequest(
        string title,
        string prompt,
        TValue initialValue,
        IValueConverter converter,
        IReadOnlyList<ValidationAttribute>? validators = null,
        string acceptLabel = "OK",
        string cancelLabel = "Cancel")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentException.ThrowIfNullOrWhiteSpace(acceptLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelLabel);

        Title = title;
        Prompt = prompt;
        InitialValue = initialValue;
        Converter = converter;
        Validators = validators?.ToArray() ?? [];
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
    }

    /// <summary>
    ///     Gets the dialog title displayed by the operating-system window chrome.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Gets the field label or editing instruction.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    ///     Gets the value formatted into the field before it receives focus.
    /// </summary>
    public TValue InitialValue { get; }

    /// <summary>
    ///     Gets the converter responsible for culture and format compatibility.
    /// </summary>
    public IValueConverter Converter { get; }

    /// <summary>
    ///     Gets an immutable snapshot of rules evaluated in order after parsing.
    /// </summary>
    public IReadOnlyList<ValidationAttribute> Validators { get; }

    /// <summary>
    ///     Gets the label for the Enter/default action.
    /// </summary>
    public string AcceptLabel { get; }

    /// <summary>
    ///     Gets the label for the Escape/cancel action.
    /// </summary>
    public string CancelLabel { get; }

    /// <summary>
    ///     Parses current field text and returns the first conversion or validation problem.
    /// </summary>
    /// <param name="text">The editable text to evaluate.</param>
    /// <returns>A typed value only when parsing and every ordered validator succeed.</returns>
    public ValueEvaluation<TValue> Evaluate(string? text)
    {
        object? converted;
        try
        {
            converted = Converter.ConvertBack(
                text,
                typeof(TValue),
                null,
                CultureInfo.InvariantCulture);
        }
        catch (FormatException exception)
        {
            return new ValueEvaluation<TValue>(
                false,
                default,
                exception.Message);
        }
        catch (InvalidCastException exception)
        {
            return new ValueEvaluation<TValue>(
                false,
                default,
                exception.Message);
        }

        TValue? value;
        if (converted is TValue typedValue)
            value = typedValue;
        else if (converted is null && default(TValue) is null)
            value = default;
        else
            return new ValueEvaluation<TValue>(
                false,
                default,
                $"The converter returned {converted?.GetType().Name ?? "null"} instead of {typeof(TValue).Name}.");

        ValidationContext context = new(this)
        {
            MemberName = nameof(InitialValue),
            DisplayName = Prompt,
        };
        foreach (var validator in Validators)
        {
            var result = validator.GetValidationResult(value, context);
            if (result != ValidationResult.Success)
                return new ValueEvaluation<TValue>(
                    false,
                    default,
                    result?.ErrorMessage);
        }

        return new ValueEvaluation<TValue>(true, value, null);
    }
}

