using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Dialogs;

[TestClass]
public sealed class CurrentBeatmapDialogServiceTests
{
    [TestMethod]
    public async Task FetchAsync_WhenLookupFails_ShowsOriginalExceptionMessage()
    {
        // Arrange
        RecordingCurrentBeatmapLocator locator = new()
        {
            Failure = new InvalidOperationException("The editor state is unavailable."),
        };
        TestDialogService dialogs = new();
        UserNotificationService notifications = new();
        var service = new CurrentBeatmapDialogService(
            locator,
            dialogs,
            new PhysicalBeatmapsetFileSystem(),
            notifications);

        // Act
        string? path = await service.FetchAsync();

        // Assert
        path.Should().BeNull();
        dialogs.MessageCount.Should().Be(1);
        dialogs.LastMessageTitle.Should().Be("Current beatmap unavailable");
        dialogs.LastMessage.Should().Be("The editor state is unavailable.");
    }

    [TestMethod]
    public async Task FetchAsync_WhenReportedFileIsMissing_PublishesWarningSnackbar()
    {
        // Arrange
        RecordingCurrentBeatmapLocator locator = new(
            Path.Combine(Path.GetTempPath(), $"mapping-tools-missing-{Guid.NewGuid():N}.osu"));
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var service = new CurrentBeatmapDialogService(
            locator,
            new TestDialogService(),
            new PhysicalBeatmapsetFileSystem(),
            notifications);

        // Act
        string? path = await service.FetchAsync();

        // Assert
        path.Should().BeNull();
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Warning
            && notification.Title == "Current beatmap is missing"
            && notification.Message.Contains(locator.Path!, StringComparison.Ordinal));
    }
}
