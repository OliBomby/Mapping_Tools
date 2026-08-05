using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Desktop.Converters;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
/// Holds a type-erased value whose text conversion remains in the Avalonia binding.
/// </summary>
public sealed class ValueDialogViewModel : ObservableValidator
{
    private readonly Func<object?, ValidationResult?> _validate;
    private readonly Action<object?> _accept;
    private readonly TextConversionState _conversionState;
    private object? _value;

    /// <summary>
    /// Creates typed dialog state and validates the initial value through DataAnnotations.
    /// </summary>
    /// <param name="title">The native window title.</param>
    /// <param name="prompt">The field instruction.</param>
    /// <param name="initialValue">The initial typed value before binding conversion.</param>
    /// <param name="acceptLabel">The Enter/default action label.</param>
    /// <param name="cancelLabel">The Escape/cancel action label.</param>
    /// <param name="validate">The typed application rules adapted to the erased value.</param>
    /// <param name="conversionState">Tracks malformed text rejected by the binding converter.</param>
    /// <param name="accept">The callback receiving the validated, type-erased value.</param>
    /// <param name="cancel">The callback that closes the dialog without a value.</param>
    public ValueDialogViewModel(
        string title,
        string prompt,
        object? initialValue,
        string acceptLabel,
        string cancelLabel,
        Func<object?, ValidationResult?> validate,
        TextConversionState conversionState,
        Action<object?> accept,
        Action cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(conversionState);
        ArgumentNullException.ThrowIfNull(accept);
        ArgumentNullException.ThrowIfNull(cancel);

        Title = title;
        Prompt = prompt;
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
        _value = initialValue;
        _validate = validate;
        _conversionState = conversionState;
        _accept = accept;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(cancel);

        ErrorsChanged += (_, _) => OnPropertyChanged(nameof(IsValid));
        _conversionState.ErrorChanged += (_, _) =>
            OnPropertyChanged(nameof(IsValid));
        ValidateProperty(_value, nameof(Value));
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
    /// Gets or sets the typed value produced by the field's binding converter.
    /// </summary>
    [DialogValue]
    public object? Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value))
            {
                return;
            }

            SetProperty(ref _value, value, validate: true);
        }
    }

    /// <summary>
    /// Gets whether conversion and every DataAnnotations rule currently succeed.
    /// </summary>
    public bool IsValid => !_conversionState.HasError && !HasErrors;

    /// <summary>
    /// Gets the label for the Enter/default action.
    /// </summary>
    public string AcceptLabel { get; }

    /// <summary>
    /// Gets the label for the Escape/cancel action.
    /// </summary>
    public string CancelLabel { get; }

    /// <summary>
    /// Gets the guarded command that submits only the currently validated value.
    /// </summary>
    public ICommand AcceptCommand { get; }

    /// <summary>
    /// Gets the command that dismisses the field without submitting a value.
    /// </summary>
    public ICommand CancelCommand { get; }

    internal ValidationResult? ValidateDialogValue(object? value) =>
        _validate(value);

    private void Accept()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(IsValid));
        if (!HasErrors && !_conversionState.HasError)
        {
            _accept(Value);
        }
    }
}
