using FluentAssertions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class BetterSaveServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_WithCurrentBeatmap_RequiresLiveStateAndSavesThroughGateway()
    {
        // Arrange
        RecordingEditingGateway gateway = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new FixedCurrentBeatmapLocator(@"C:\Songs\current.osu"),
            gateway,
            notifications);

        // Act
        BetterSaveResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.Saved);
        result.Path.Should().Be(@"C:\Songs\current.osu");
        gateway.OpenedPath.Should().Be(@"C:\Songs\current.osu");
        gateway.LivePreference.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SavedEditor.Should().BeSameAs(gateway.Session.Editor);
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithoutCurrentBeatmap_ReturnsTypedWarningWithoutOpening()
    {
        // Arrange
        RecordingEditingGateway gateway = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new FixedCurrentBeatmapLocator(null),
            gateway,
            notifications);

        // Act
        BetterSaveResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.NoCurrentBeatmap);
        gateway.OpenedPath.Should().BeNull();
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Warning);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenGatewayFails_CapturesFailureAndPublishesError()
    {
        // Arrange
        IOException failure = new("Live editor unavailable.");
        RecordingEditingGateway gateway = new() { Failure = failure };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        BetterSaveService service = new(
            new FixedCurrentBeatmapLocator(@"C:\Songs\current.osu"),
            gateway,
            notifications);

        // Act
        BetterSaveResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(BetterSaveStatus.Failed);
        result.Exception.Should().BeSameAs(failure);
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Error &&
            notification.Exception == failure);
    }

    private sealed class FixedCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        private readonly string? _path;

        public FixedCurrentBeatmapLocator(string? path)
        {
            _path = path;
        }

        public Task<string?> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_path);
        }
    }

    private sealed class RecordingEditingGateway : IBeatmapEditingGateway
    {
        public RecordingEditingGateway()
        {
            BeatmapEditor2 editor = new(
                ["osu file format v14", "", "[HitObjects]"],
                new MemoryTextFileStore());
            Session = new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.LiveEditor,
                []);
        }

        public BeatmapEditingSession Session { get; }

        public string? OpenedPath { get; private set; }

        public LiveBeatmapPreference? LivePreference { get; private set; }

        public Editor2? SavedEditor { get; private set; }

        public Exception? Failure { get; init; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedPath = path;
            LivePreference = livePreference;
            return Failure is null
                ? Task.FromResult(Session)
                : Task.FromException<BeatmapEditingSession>(Failure);
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Editor2 editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SavedEditor = editor;
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            SaveAsync(session.Editor, reloadEditor, cancellationToken);
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => [];

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path) => string.Empty;

        public string CombinePath(string parent, string child) => child;
    }
}
