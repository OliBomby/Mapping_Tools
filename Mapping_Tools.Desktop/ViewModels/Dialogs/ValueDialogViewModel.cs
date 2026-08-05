using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Desktop.ViewModels.Dialogs.Validation;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
/// Holds a type-erased value whose text conversion remains in the Avalonia binding.
/// </summary>
public sealed partial class ValueDialogViewModel : ObservableValidator
{
    private readonly Func<object?, ValidationResult?> _validate;
    private readonly Action<object?> _accept;
    private readonly Action _cancel;
    private bool _hasConversionError;
    /// <summary>
    /// Gets or sets the typed value produced by the field's binding converter.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [DialogValue]
    public partial object? Value { get; set; }

    /// <summary>
    /// Creates typed dialog state and validates the initial value through DataAnnotations.
    /// </summary>
    /// <param name="title">The native window title.</param>
    /// <param name="prompt">The field instruction.</param>
    /// <param name="initialValue">The initial typed value before binding conversion.</param>
    /// <param name="acceptLabel">The Enter/default action label.</param>
    /// <param name="cancelLabel">The Escape/cancel action label.</param>
    /// <param name="validate">The typed application rules adapted to the erased value.</param>
    /// <param name="accept">The callback receiving the validated, type-erased value.</param>
    /// <param name="cancel">The callback that closes the dialog without a value.</param>
    public ValueDialogViewModel(
        string title,
        string prompt,
        object? initialValue,
        string acceptLabel,
        string cancelLabel,
        Func<object?, ValidationResult?> validate,
        Action<object?> accept,
        Action cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(accept);
        ArgumentNullException.ThrowIfNull(cancel);

        Title = title;
        Prompt = prompt;
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
        _validate = validate;
        _accept = accept;
        _cancel = cancel;

        ErrorsChanged += (_, _) => OnPropertyChanged(nameof(IsValid));
        if (Equals(Value, initialValue))
        {
            ValidateProperty(Value, nameof(Value));
        }
        else
        {
            Value = initialValue;
        }
    }

    /// <summary>
    /// Gets the native window title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the label or editing instruction above the field.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// Gets whether conversion and every DataAnnotations rule currently succeed.
    /// </summary>
    public bool IsValid => !_hasConversionError && !HasErrors;

    /// <summary>
    /// Gets the label for the Enter/default action.
    /// </summary>
    public string AcceptLabel { get; }

    /// <summary>
    /// Gets the label for the Escape/cancel action.
    /// </summary>
    public string CancelLabel { get; }

    internal ValidationResult? ValidateDialogValue(object? value) =>
        _validate(value);

    internal void SetConversionError(string? errorMessage)
    {
        bool hasError = errorMessage is not null;
        if (_hasConversionError == hasError)
        {
            return;
        }

        _hasConversionError = hasError;
        OnPropertyChanged(nameof(IsValid));
    }

    [RelayCommand]
    private void Accept()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(IsValid));
        if (!HasErrors && !_hasConversionError)
        {
            _accept(Value);
        }
    }

    [RelayCommand]
    private void Cancel() =>
        _cancel();
}
