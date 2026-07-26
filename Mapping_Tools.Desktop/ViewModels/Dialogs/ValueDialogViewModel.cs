using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
/// Holds an erased typed value and its validation status for the reusable field dialog.
/// </summary>
public sealed class ValueDialogViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Func<string?, ValueInputEvaluation> _evaluate;
    private readonly Action<object?> _accept;
    private string _text;
    private string? _errorMessage;
    private bool _isValid;
    private object? _parsedValue;

    /// <summary>
    /// Notifies the bound field when parsing or validation changes so its
    /// Material template can present the current correction.
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Creates editable dialog state and immediately validates the formatted initial text.
    /// </summary>
    /// <param name="title">The native window title.</param>
    /// <param name="prompt">The field instruction.</param>
    /// <param name="initialText">The converter-formatted initial value.</param>
    /// <param name="acceptLabel">The Enter/default action label.</param>
    /// <param name="cancelLabel">The Escape/cancel action label.</param>
    /// <param name="evaluate">The parser and ordered validation pipeline.</param>
    /// <param name="accept">The callback receiving the validated, type-erased value.</param>
    /// <param name="cancel">The callback that closes the dialog without a value.</param>
    public ValueDialogViewModel(
        string title,
        string prompt,
        string initialText,
        string acceptLabel,
        string cancelLabel,
        Func<string?, ValueInputEvaluation> evaluate,
        Action<object?> accept,
        Action cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(evaluate);
        ArgumentNullException.ThrowIfNull(accept);
        ArgumentNullException.ThrowIfNull(cancel);

        Title = title;
        Prompt = prompt;
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
        _text = initialText;
        _evaluate = evaluate;
        _accept = accept;
        AcceptCommand = ReactiveCommand.Create(Accept);
        CancelCommand = ReactiveCommand.Create(cancel);
        Evaluate();
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
    /// Gets or sets the editable representation and revalidates it immediately.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _text, value) is not null)
            {
                Evaluate();
            }
        }
    }

    /// <summary>
    /// Gets whether the current text parsed and passed every validation rule.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isValid, value);
        }
    }

    /// <summary>
    /// Gets whether the editable representation currently fails parsing or
    /// one of the dialog request's validation rules.
    /// </summary>
    public bool HasErrors => _errorMessage is not null;

    /// <summary>
    /// Returns the correction associated with <see cref="Text"/> for
    /// Avalonia's binding-validation pipeline.
    /// </summary>
    /// <param name="propertyName">
    /// The requested property, or an empty value for all dialog errors.
    /// </param>
    /// <returns>
    /// An empty snapshot when valid; otherwise the single current correction.
    /// </returns>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName)
            && propertyName != nameof(Text))
        {
            return Array.Empty<string>();
        }

        return _errorMessage is null
            ? Array.Empty<string>()
            : new[] { _errorMessage };
    }

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

    private void Evaluate()
    {
        ValueInputEvaluation result = _evaluate(Text);
        _parsedValue = result.Value;
        SetValidationError(result.ErrorMessage);
        IsValid = result.IsValid;
    }

    private void SetValidationError(string? error)
    {
        if (_errorMessage == error)
        {
            return;
        }

        _errorMessage = error;
        this.RaisePropertyChanged(nameof(HasErrors));
        ErrorsChanged?.Invoke(
            this,
            new DataErrorsChangedEventArgs(nameof(Text)));
    }

    private void Accept()
    {
        if (IsValid)
        {
            _accept(_parsedValue);
        }
    }
}

/// <summary>
/// Transfers a type-erased parse result from a generic dialog request into presentation state.
/// </summary>
/// <param name="IsValid">Whether the value may be submitted.</param>
/// <param name="Value">The parsed value when valid.</param>
/// <param name="ErrorMessage">The first user-facing parsing or validation problem.</param>
public readonly record struct ValueInputEvaluation(
    bool IsValid,
    object? Value,
    string? ErrorMessage);
