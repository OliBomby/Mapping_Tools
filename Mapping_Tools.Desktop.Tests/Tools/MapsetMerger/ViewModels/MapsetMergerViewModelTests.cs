using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.MapsetMerger.ViewModels;

[TestClass]
public sealed class MapsetMergerViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithDuplicateNames_UpdatesVisibleNamesBeforeCallingService()
    {
        // Arrange
        RecordingMapsetMergerService service = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(service, notifications: notifications);
        viewModel.Mapsets.Add(new MapsetMergerItemViewModel(new TestFilePicker(), "Pack", "first"));
        viewModel.Mapsets.Add(new MapsetMergerItemViewModel(new TestFilePicker(), "Pack", "second"));

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        viewModel.Mapsets.Select(item => item.Name).Should().Equal("Pack", "Pack1");
        service.Project.Should().NotBeNull();
        service.Project!.Mapsets.Select(item => item.Name).Should().Equal("Pack", "Pack1");
        published.Should().ContainSingle(notification => notification.Message == "Successfully merged 2 mapsets!");
    }

    [TestMethod]
    public async Task BrowseExportPathCommand_WithCurrentFolderDisabled_UsesCurrentExportPath()
    {
        // Arrange
        TestFilePicker filePicker = new() { Folders = [@"D:\Chosen"] };
        TestBeatmapWorkspace workspace = new();
        var viewModel = CreateViewModel(
            new RecordingMapsetMergerService(),
            filePicker,
            workspace);

        // Act
        await viewModel.BrowseExportPathCommand.ExecuteAsync(null);

        // Assert
        filePicker.LastFolderRequest!.SuggestedStartLocation.Should().Be(
            @"C:\Local\Mapping Tools\Exports");
    }

    [TestMethod]
    public async Task BrowseExportPathCommand_WithCurrentFolderEnabled_UsesSelectedBeatmapDirectory()
    {
        // Arrange
        TestFilePicker filePicker = new() { Folders = [@"D:\Chosen"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = CreateViewModel(
            new RecordingMapsetMergerService(),
            filePicker,
            workspace);

        // Act
        await viewModel.BrowseExportPathCommand.ExecuteAsync(null);

        // Assert
        filePicker.LastFolderRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    private static MapsetMergerViewModel CreateViewModel(
        RecordingMapsetMergerService service,
        TestFilePicker? filePicker = null,
        TestBeatmapWorkspace? workspace = null,
        IUserNotificationService? notifications = null)
    {
        var notificationService = notifications ?? new UserNotificationService();
        ToolExecutionService execution = new(
            notificationService,
            TimeProvider.System);
        return new MapsetMergerViewModel(
            service,
            execution,
            filePicker ?? new TestFilePicker(),
            workspace ?? new TestBeatmapWorkspace(),
            new TestCurrentBeatmapDialogService(),
            new TestApplicationDirectories(),
            notificationService);
    }

    private sealed class RecordingMapsetMergerService : IMapsetMergerService
    {
        public MapsetMergerServiceOptions? Project { get; private set; }

        public Task<MapsetMergerResult> MergeAsync(
            MapsetMergerServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Project = project;
            return Task.FromResult(new MapsetMergerResult(2, 0, 0, 0));
        }
    }
}
