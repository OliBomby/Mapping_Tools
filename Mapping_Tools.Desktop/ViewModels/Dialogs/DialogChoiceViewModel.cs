using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
/// Exposes one message-dialog action with its keyboard role and close command.
/// </summary>
public sealed class DialogChoiceViewModel : ObservableObject
{
    /// <summary>
    /// Creates an action that invokes the supplied close callback once per execution.
    /// </summary>
    /// <param name="label">The concise button text.</param>
    /// <param name="isDefault">Whether Enter should invoke the action.</param>
    /// <param name="isCancel">Whether Escape should invoke the action.</param>
    /// <param name="close">The callback that closes the owning dialog with its typed result.</param>
    public DialogChoiceViewModel(
        string label,
        bool isDefault,
        bool isCancel,
        Action close)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(close);
        Label = label;
        IsDefault = isDefault;
        IsCancel = isCancel;
        Command = new RelayCommand(close);
    }

    /// <summary>
    /// Gets the text displayed on the action button.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets whether Enter invokes this action while the dialog is active.
    /// </summary>
    public bool IsDefault { get; }

    /// <summary>
    /// Gets whether Escape invokes this action while the dialog is active.
    /// </summary>
    public bool IsCancel { get; }

    /// <summary>
    /// Gets the action that returns the associated result and closes the dialog.
    /// </summary>
    public ICommand Command { get; }
}
