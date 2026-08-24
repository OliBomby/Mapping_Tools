using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.BeatmapEditing;

[TestClass]
public sealed class BetterSaveServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_WithCurrentBeatmap_RequiresLiveStateAndSavesThroughGateway()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway =
            new(CreateSession());
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new RecordingCurrentBeatmapLocator(@"C:\Songs\current.osu"),
            gateway,
            notifications);

        // Act
        var result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.Saved);
        result.Path.Should().Be(@"C:\Songs\current.osu");
        gateway.OpenRequests.Single().Path.Should().Be(@"C:\Songs\current.osu");
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SessionSaveRequests.Single().Session.Editor.Should().BeSameAs(gateway.Session!.Editor);
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithoutCurrentBeatmap_ReturnsTypedWarningWithoutOpening()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new RecordingCurrentBeatmapLocator(),
            gateway,
            notifications);

        // Act
        var result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.NoCurrentBeatmap);
        gateway.OpenRequests.Should().BeEmpty();
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Warning);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenGatewayFails_CapturesFailureAndPublishesError()
    {
        // Arrange
        IOException failure = new("Live editor unavailable.");
        RecordingBeatmapEditingGateway gateway = new()
        {
            OpenBeatmapFailure = failure,
        };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new RecordingCurrentBeatmapLocator(@"C:\Songs\current.osu"),
            gateway,
            notifications);

        // Act
        var result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.Failed);
        result.Exception.Should().BeSameAs(failure);
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Error && notification.Exception == failure);
    }

    private static BeatmapEditingSession CreateSession()
    {
        BeatmapEditor editor = new(
            ["osu file format v14", "", "[HitObjects]"],
            new NoOpTextFileStore { ReadResult = [] });
        return new BeatmapEditingSession(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
    }
}
