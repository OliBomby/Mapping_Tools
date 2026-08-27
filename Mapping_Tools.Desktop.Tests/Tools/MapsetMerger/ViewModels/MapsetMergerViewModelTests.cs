using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
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
        var viewModel = CreateViewModel(service);
        viewModel.Mapsets.Add(new MapsetMergerItemViewModel(new TestFilePicker(), "Pack", "first"));
        viewModel.Mapsets.Add(new MapsetMergerItemViewModel(new TestFilePicker(), "Pack", "second"));

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        viewModel.Mapsets.Select(item => item.Name).Should().Equal("Pack", "Pack1");
        service.Project.Should().NotBeNull();
        service.Project!.Mapsets.Select(item => item.Name).Should().Equal("Pack", "Pack1");
    }

    private static MapsetMergerViewModel CreateViewModel(RecordingMapsetMergerService service)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new MapsetMergerViewModel(
            service,
            execution,
            new TestFilePicker(),
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator(),
            new TestApplicationDirectories());
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
