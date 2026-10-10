using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Hosted;

[TestClass]
public sealed class GlobalHotkeyHostedServiceTests
{
    [TestMethod]
    public async Task StartAsync_DesktopRegistrationFails_ShowsLocalizedSnackbarWithGuidanceAndRetainsDiagnostics()
    {
        // Arrange
        UserNotificationService notifications = new();
        TestDialogService dialogs = new();
        RecordingNotificationSurface surface = new();
        NotificationPresenter presenter = new(notifications, dialogs, surface);
        await presenter.StartAsync(CancellationToken.None);
        RecordingRegistration hotkeys = new();
        GlobalHotkeyHostedService service = CreateService(hotkeys, notifications);

        try
        {
            // Act
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
            hotkeys.ReportFailure();

            // Assert
            surface.Notifications.Should().ContainSingle().Which.Should().BeEquivalentTo(new UserNotification(
                UserNotificationSeverity.Warning, DesktopStrings.Shell_GlobalShortcuts,
                DesktopStrings.Shell_GlobalShortcutsUnavailable, hotkeys.Failure));
            surface.Notifications[0].Exception.Should().BeSameAs(hotkeys.Failure);
            surface.Notifications[0].Message.Should().Contain("xdg-desktop-portal-kde");
            dialogs.MessageCount.Should().Be(0);
        }
        finally
        {
            await presenter.StopAsync(CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task StartAsync_NoConfiguredShortcuts_DoesNotShowRegistrationWarning()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        RecordingRegistration hotkeys = new();
        GlobalHotkeyHostedService service = new(hotkeys, new UnusedQuickRunService(), new TestQuickUndoCommandService(),
            new TestBetterSaveService(), new DesktopApplicationSettings { QuickRunHotkey = new HotkeySettings(0, 2) },
            notifications: notifications);

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert
        published.Should().BeEmpty();
    }

    private static GlobalHotkeyHostedService CreateService(RecordingRegistration hotkeys, IUserNotificationService notifications)
    {
        return new GlobalHotkeyHostedService(hotkeys, new UnusedQuickRunService(), new TestQuickUndoCommandService(),
            new TestBetterSaveService(), new DesktopApplicationSettings { QuickRunHotkey = new HotkeySettings(44, 2) },
            notifications: notifications);
    }

    private sealed class RecordingRegistration : IGlobalHotkeyService, IGlobalHotkeyRegistration
    {
        public Exception Failure { get; } = new IOException("Desktop registration failed");
        public event EventHandler<Exception>? RegistrationFailed;

        public void ReportFailure() => RegistrationFailed?.Invoke(this, Failure);
        public void Start() => ReportFailure();
        public void Stop() { }
        public void SetBinding(string id, HotkeySettings? hotkey, Func<CancellationToken, Task> callback) { }

        public Task<Dictionary<string, HotkeySettings>> GetRegisteredShortcutsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Dictionary<string, HotkeySettings> { ["quick-run"] = new(97, 8) });
        }
    }

    private sealed class RecordingNotificationSurface : INotificationSurface
    {
        public List<UserNotification> Notifications { get; } = [];
        public void ShowSnackbar(UserNotification notification) => Notifications.Add(notification);
    }

    private sealed class UnusedQuickRunService : IQuickRunService
    {
        public Task<QuickRunResult> RunAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("This test does not invoke shortcuts.");
        }
    }
}
