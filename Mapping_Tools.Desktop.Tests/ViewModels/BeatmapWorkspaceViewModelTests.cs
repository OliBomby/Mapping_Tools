using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class BeatmapWorkspaceViewModelTests
{
    [TestMethod]
    public void Constructor_WithRecentMap_RestoresSelectionAndFormatsShellState()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetRecentMaps(new RecentBeatmap(
            @"C:\Songs\Mapset\Artist - Title [Hard].osu",
            "today"));

        // Act
        using BeatmapWorkspaceViewModel viewModel = CreateViewModel(workspace);

        // Assert
        workspace.LastSelectionSource.Should().Be(BeatmapSelectionSource.Startup);
        viewModel.SelectedMapNames.Should().Be("Artist - Title [Hard].osu");
        viewModel.SelectedMapToolTip.Should().Be(@"C:\Songs\Mapset\Artist - Title [Hard].osu");
        viewModel.SelectedMapCountText.Should().Be("(1) map total");
        viewModel.HasSingleSelection.Should().BeTrue();
    }

    [TestMethod]
    public void SetDroppedPaths_WithMultipleFiles_PreservesOrderAndSource()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        using BeatmapWorkspaceViewModel viewModel = CreateViewModel(workspace);

        // Act
        viewModel.SetDroppedPaths([@"C:\one.osu", @"C:\two.osu"]);

        // Assert
        workspace.SelectedPaths.Should().Equal(@"C:\one.osu", @"C:\two.osu");
        workspace.LastSelectionSource.Should().Be(BeatmapSelectionSource.DragAndDrop);
        viewModel.SelectedMapNames.Should().Be("one.osu|two.osu");
        viewModel.SelectedMapCountText.Should().Be("(2) maps total");
    }

    [TestMethod]
    public async Task CreateBackupCommand_WithSelection_ForcesUserBackupAndPublishesSuccess()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection([@"C:\map.osu"]);
        TestBeatmapBackupService backups = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        using BeatmapWorkspaceViewModel viewModel = CreateViewModel(
            workspace,
            backups,
            notifications: notifications);

        // Act
        await viewModel.CreateBackupCommand.ExecuteAsync(null);

        // Assert
        backups.CreateRequests.Should().ContainSingle();
        backups.CreateRequests[0].Paths.Should().Equal(@"C:\map.osu");
        backups.CreateRequests[0].Reason.Should().Be(BeatmapBackupReason.User);
        backups.CreateRequests[0].Force.Should().BeTrue();
        published.Should().ContainSingle(notification =>
            notification.Severity == UserNotificationSeverity.Success &&
            notification.Title == "Backup created");
    }

    [TestMethod]
    public async Task RestoreBackupCommand_WithMetadataMismatchAndOverride_RetriesExplicitly()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection([@"C:\current.osu"]);
        TestBeatmapBackupService backups = new()
        {
            RejectFirstRestoreAsIncompatible = true
        };
        TestFilePicker picker = new()
        {
            OpenFiles = [@"C:\Backups\chosen.osu"]
        };
        TestDialogService dialogs = new() { BooleanResult = true };
        using BeatmapWorkspaceViewModel viewModel = CreateViewModel(
            workspace,
            backups,
            picker,
            dialogs);

        // Act
        await viewModel.RestoreBackupCommand.ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().Be(1);
        backups.RestoreRequests.Should().HaveCount(2);
        backups.RestoreRequests.Select(request => request.AllowDifferentFilename)
            .Should().Equal(false, true);
        backups.RestoreRequests.Should().OnlyContain(request =>
            request.Backup == @"C:\Backups\chosen.osu" &&
            request.Destination == @"C:\current.osu");
    }

    [TestMethod]
    public async Task OpenCurrentBeatmapCommand_WhenLookupUnavailable_PublishesWarning()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        using BeatmapWorkspaceViewModel viewModel = CreateViewModel(
            workspace,
            notifications: notifications);

        // Act
        await viewModel.OpenCurrentBeatmapCommand.ExecuteAsync(null);

        // Assert
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Warning);
        published[0].Title.Should().Be("Current beatmap unavailable");
    }

    private static BeatmapWorkspaceViewModel CreateViewModel(
        TestBeatmapWorkspace workspace,
        TestBeatmapBackupService? backups = null,
        TestFilePicker? picker = null,
        TestDialogService? dialogs = null,
        IUserNotificationService? notifications = null)
    {
        ApplicationSettings settings = new()
        {
            BackupsPath = @"C:\Backups"
        };
        return new BeatmapWorkspaceViewModel(
            workspace,
            backups ?? new TestBeatmapBackupService(),
            new TestQuickUndoCommandService(),
            picker ?? new TestFilePicker(),
            new TestFileRevealService(),
            new TestApplicationDirectories(),
            settings,
            dialogs ?? new TestDialogService(),
            notifications ?? new UserNotificationService(),
            new ImmediateTestDispatcher());
    }
}
