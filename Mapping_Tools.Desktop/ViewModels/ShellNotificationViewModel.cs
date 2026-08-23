using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Presents one independently dismissible shell notification.
/// </summary>
public sealed partial class ShellNotificationViewModel : ObservableObject
{
    private readonly Action<ShellNotificationViewModel> dismiss;

    internal ShellNotificationViewModel(
        UserNotification notification,
        Action<ShellNotificationViewModel> dismiss)
    {
        ArgumentNullException.ThrowIfNull(notification);
        Severity = notification.Severity;
        Title = notification.Title;
        Message = notification.Message;
        this.dismiss = dismiss;
    }

    /// <summary>Gets the notification severity.</summary>
    public UserNotificationSeverity Severity { get; }

    /// <summary>Gets the compact notification heading.</summary>
    public string Title { get; }

    /// <summary>Gets the notification body.</summary>
    public string Message { get; }

    /// <summary>Gets the compact legacy-snackbar text.</summary>
    public string DisplayText => $"{Title}: {Message}";

    [RelayCommand]
    private void Dismiss()
    {
        dismiss(this);
    }
}
