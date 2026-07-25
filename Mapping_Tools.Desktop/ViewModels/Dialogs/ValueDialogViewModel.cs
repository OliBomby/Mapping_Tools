using System.Windows.Input;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
/// Holds an erased typed value and its validation status for the reusable field dialog.
/// </summary>
public sealed class ValueDialogViewModel : ViewModelBase
{
    private readonly Func<string?, ValueInputEvaluation> _evaluate;
    private readonly Action<object?> _accept;
    private string _text;
    private string? _errorMessage;
    private bool _isValid;
    private object? _parsedValue;

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
    /// Gets the first parsing or validation error, or <see langword="null"/> when submission is allowed.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
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
            this.RaisePropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Gets whether correction text should occupy space below the field.
    /// </summary>
    public bool HasError => !IsValid;

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
        ErrorMessage = result.ErrorMessage;
        IsValid = result.IsValid;
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
