using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Desktop.Validation;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>Owns editable dialog text and validates its converted value through DataAnnotations.</summary>
public sealed partial class ValueDialogViewModel : ObservableValidator
{
    private readonly Action<object?> accept;
    private readonly Action cancel;
    private readonly IValueConverter converter;
    private readonly Type targetType;
    private readonly Func<object?, ValidationResult?> validate;
    private object? parsedValue;

    /// <summary>Creates typed dialog state and validates the formatted initial value.</summary>
    /// <param name="title">The native window title.</param>
    /// <param name="prompt">The field instruction.</param>
    /// <param name="initialValue">The initial typed value to format.</param>
    /// <param name="converter">The shared Desktop text converter.</param>
    /// <param name="targetType">The typed value expected after parsing.</param>
    /// <param name="acceptLabel">The Enter/default action label.</param>
    /// <param name="cancelLabel">The Escape/cancel action label.</param>
    /// <param name="validate">The typed application rules adapted to the erased value.</param>
    /// <param name="accept">The callback receiving the validated value.</param>
    /// <param name="cancel">The callback that closes the dialog without a value.</param>
    public ValueDialogViewModel(
        string title,
        string prompt,
        object? initialValue,
        IValueConverter converter,
        Type targetType,
        string acceptLabel,
        string cancelLabel,
        Func<object?, ValidationResult?> validate,
        Action<object?> accept,
        Action cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(accept);
        ArgumentNullException.ThrowIfNull(cancel);

        Title = title;
        Prompt = prompt;
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
        this.converter = converter;
        this.targetType = targetType;
        this.validate = validate;
        this.accept = accept;
        this.cancel = cancel;
        ValueText = converter.Convert(initialValue, typeof(string), null, CultureInfo.InvariantCulture)?.ToString()
                    ?? string.Empty;
        ErrorsChanged += (_, _) => OnPropertyChanged(nameof(IsValid));
        ValidateProperty(ValueText, nameof(ValueText));
    }

    /// <summary>Gets or sets the editable invariant text displayed in the value field.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [DialogValue]
    public partial string ValueText { get; set; }

    /// <summary>Gets the native window title.</summary>
    public string Title { get; }

    /// <summary>Gets the label or editing instruction above the field.</summary>
    public string Prompt { get; }

    /// <summary>Gets whether conversion and every DataAnnotations rule currently succeed.</summary>
    public bool IsValid => !HasErrors;

    /// <summary>Gets the label for the Enter/default action.</summary>
    public string AcceptLabel { get; }

    /// <summary>Gets the label for the Escape/cancel action.</summary>
    public string CancelLabel { get; }

    internal ValidationResult? ValidateDialogText(object? value)
    {
        object? converted;
        try
        {
            converted = converter.ConvertBack(
                value?.ToString(),
                targetType,
                null,
                CultureInfo.InvariantCulture);
        }
        catch (FormatException exception)
        {
            return new ValidationResult(exception.Message);
        }
        catch (InvalidCastException exception)
        {
            return new ValidationResult(exception.Message);
        }

        if (converted is BindingNotification notification)
            return new ValidationResult(notification.Error?.Message ?? "The value could not be converted.");

        var result = validate(converted);
        if (result == ValidationResult.Success) parsedValue = converted;

        return result;
    }

    [RelayCommand]
    private void Accept()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(IsValid));
        if (!HasErrors) accept(parsedValue);
    }

    [RelayCommand]
    private void Cancel()
    {
        cancel();
    }
}
