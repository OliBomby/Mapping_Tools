using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Desktop.Services.Dialogs;

/// <summary>
///     Coordinates current-beatmap lookup feedback for Desktop view models.
/// </summary>
public sealed class CurrentBeatmapDialogService : ICurrentBeatmapDialogService
{
    private static string DialogTitle => DesktopStrings.Shell_CurrentBeatmapUnavailable;

    private readonly IDialogService dialogs;
    private readonly IBeatmapsetFileSystem fileSystem;
    private readonly ICurrentBeatmapLocator locator;
    private readonly IUserNotificationService notifications;

    /// <summary>Creates the shared current-beatmap feedback service.</summary>
    /// <param name="locator">Resolves the beatmap currently open in osu!.</param>
    /// <param name="dialogs">Presents modal error dialogs.</param>
    /// <param name="fileSystem">Checks whether the reported beatmap still exists.</param>
    /// <param name="notifications">Publishes warning snackbars.</param>
    public CurrentBeatmapDialogService(
        ICurrentBeatmapLocator locator,
        IDialogService dialogs,
        IBeatmapsetFileSystem fileSystem,
        IUserNotificationService notifications)
    {
        this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc />
    public async Task<string?> FetchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path;
        try
        {
            path = await locator.FindCurrentBeatmapAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception, cancellationToken);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (fileSystem.FileExists(path)) return path;

        await notifications.PublishAsync(
            new UserNotification(
                UserNotificationSeverity.Warning,
                DesktopStrings.Shell_CurrentBeatmapIsMissing,
                ApplicationText.Format(DesktopStrings.Shell_PathMissing, path)),
            cancellationToken);
        return null;
    }

    private Task ShowErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        return dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                DialogTitle,
                ApplicationExceptionText.GetSummary(exception, ApplicationStrings.Exception_LiveEditorUnavailable),
                [new DialogChoice<bool>(DesktopStrings.Shell_UpperOk, true, true, true)],
                true,
                exception.ToString()),
            cancellationToken);
    }
}
