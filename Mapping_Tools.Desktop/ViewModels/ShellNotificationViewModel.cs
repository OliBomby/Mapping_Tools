using Avalonia.Media;
using Mapping_Tools.ApplicationServices.Execution;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Presents one independently dismissible shell notification.
/// </summary>
public sealed class ShellNotificationViewModel : ViewModelBase
{
    internal ShellNotificationViewModel(
        UserNotification notification,
        Action<ShellNotificationViewModel> dismiss)
    {
        ArgumentNullException.ThrowIfNull(notification);
        Severity = notification.Severity;
        Title = notification.Title;
        Message = notification.Message;
        DismissCommand = ReactiveCommand.Create(() => dismiss(this));
    }

    /// <summary>Gets the notification severity.</summary>
    public UserNotificationSeverity Severity { get; }

    /// <summary>Gets the compact notification heading.</summary>
    public string Title { get; }

    /// <summary>Gets the notification body.</summary>
    public string Message { get; }

    /// <summary>Gets the uppercase severity label shown to every visual theme.</summary>
    public string SeverityLabel => Severity.ToString().ToUpperInvariant();

    /// <summary>Gets the compact legacy-snackbar text.</summary>
    public string DisplayText => $"{Title}: {Message}";

    /// <summary>Gets the severity-specific accent used by the notification surface.</summary>
    public IBrush AccentBrush => Severity switch
    {
        UserNotificationSeverity.Success => Brushes.LimeGreen,
        UserNotificationSeverity.Warning => Brushes.Orange,
        UserNotificationSeverity.Error => Brushes.IndianRed,
        _ => Brushes.CornflowerBlue
    };

    /// <summary>Gets the command that removes this notification from the queue.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> DismissCommand { get; }
}
