using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Notifications;

[TestClass]
public sealed class NotificationPresenterTests
{
    [TestMethod]
    public async Task OnNotificationPublished_ErrorNotification_ShowsErrorMessageDialog()
    {
        // Arrange
        UserNotificationService notifications = new();
        TestDialogService dialogs = new();
        TestNotificationSurface surface = new();
        NotificationPresenter presenter = new(notifications, dialogs, surface);
        await presenter.StartAsync(CancellationToken.None);
        Exception exception = new InvalidOperationException(
            "Operation failed",
            new ArgumentException("Inner failure"));

        // Act
        await notifications.PublishAsync(
            new UserNotification(
                UserNotificationSeverity.Error,
                "Test tool",
                exception.Message,
                exception));

        // Assert
        MessageDialogRequest<bool> request = dialogs.LastMessageRequest
            .Should()
            .BeOfType<MessageDialogRequest<bool>>()
            .Subject;
        request.Title.Should().Be("Test tool Error");
        request.Message.Should().Be("Operation failed");
        request.Choices.Should().ContainSingle();
        request.Choices[0].Label.Should().Be("OK");
        request.Choices[0].IsDefault.Should().BeTrue();
        request.Choices[0].IsCancel.Should().BeTrue();
        request.Details.Should().Contain("Operation failed");
        request.Details.Should().Contain("Inner exception:");
        request.Details.Should().Contain("Inner failure");
        surface.SnackbarCount.Should().Be(0);
        await presenter.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task OnNotificationPublished_InformationNotification_ShowsSnackbar()
    {
        // Arrange
        UserNotificationService notifications = new();
        TestDialogService dialogs = new();
        TestNotificationSurface surface = new();
        NotificationPresenter presenter = new(notifications, dialogs, surface);
        await presenter.StartAsync(CancellationToken.None);

        // Act
        await notifications.PublishAsync(
            new UserNotification(
                UserNotificationSeverity.Information,
                "Test tool",
                "Operation completed."));

        // Assert
        surface.SnackbarCount.Should().Be(1);
        dialogs.MessageCount.Should().Be(0);
        await presenter.StopAsync(CancellationToken.None);
    }

    private sealed class TestNotificationSurface : INotificationSurface
    {
        public int SnackbarCount { get; private set; }

        public void ShowSnackbar(UserNotification notification)
        {
            SnackbarCount++;
        }
    }
}
