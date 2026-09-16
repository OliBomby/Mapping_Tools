using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.RhythmGuide.ViewModels;

[TestClass]
public sealed class RhythmGuideViewModelTests
{
    [TestMethod]
    public async Task BrowseSourcesCommand_WithMultipleFiles_PreservesPickerOrderAndCount()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = CreateViewModel(filePicker: picker, workspace: workspace);

        // Act
        await ExecuteAsync(viewModel.BrowseSourcesCommand);

        // Assert
        viewModel.SourcePaths.Should().Equal("first.osu", "second.osu");
        viewModel.SourceCount.Should().Be(2);
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
        picker.LastOpenRequest.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task RunCommand_WithNewMap_ExecutesUseCaseWithoutCompletionMessageOrReveal()
    {
        // Arrange
        RecordingRhythmGuideService rhythmGuide = new();
        var viewModel = CreateViewModel(rhythmGuide);
        viewModel.SourcePaths = ["source.osu"];
        viewModel.ExportPath = "guide.osu";

        // Act
        await ExecuteAsync(viewModel.RunCommand);

        // Assert
        rhythmGuide.Options.Should().NotBeNull();
        rhythmGuide.Options!.Paths.Should().Equal("source.osu");
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task BrowseExportCommand_WithNewMap_UsesBeatmapOpenPicker()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["target.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = CreateViewModel(filePicker: picker, workspace: workspace);
        viewModel.ExportMode = RhythmGuideExportMode.NewMap;

        // Act
        await ExecuteAsync(viewModel.BrowseExportCommand);

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeFalse();
        picker.LastOpenRequest.SuggestedStartLocation.Should().Be(@"C:\Maps");
        viewModel.ExportPath.Should().Be("target.osu");
    }

    [TestMethod]
    public async Task RunCommand_WithAddToMap_PublishesLegacyDoneMessage()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(notifications: notifications);
        viewModel.SourcePaths = ["source.osu"];
        viewModel.ExportPath = "target.osu";
        viewModel.ExportMode = RhythmGuideExportMode.AddToMap;

        // Act
        await ExecuteAsync(viewModel.RunCommand);

        // Assert
        published.Should().ContainSingle(notification => notification.Message == "Done!");
    }

    private static RhythmGuideViewModel CreateViewModel(
        RecordingRhythmGuideService? rhythmGuide = null,
        TestFilePicker? filePicker = null,
        UserNotificationService? notifications = null,
        TestBeatmapWorkspace? workspace = null)
    {
        notifications ??= new UserNotificationService();
        ToolExecutionService execution = new(
            notifications,
            TimeProvider.System);
        return new RhythmGuideViewModel(
            rhythmGuide ?? new RecordingRhythmGuideService(),
            execution,
            filePicker ?? new TestFilePicker(),
            new RecordingCurrentBeatmapLocator(),
            workspace ?? new TestBeatmapWorkspace(),
            new StubRhythmGuideWindowService(),
            new TestApplicationDirectories());
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

    private sealed class RecordingRhythmGuideService : IRhythmGuideService
    {
        public RhythmGuideServiceOptions.RhythmGuideRunOptions? Options { get; private set; }

        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideServiceOptions.RhythmGuideRunOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new RhythmGuideResult(
                options.ExportPath,
                12,
                options.ExportMode));
        }
    }

    private sealed class StubRhythmGuideWindowService : IRhythmGuideWindowService
    {
        public void Show(RhythmGuideViewModel viewModel)
        {
        }
    }
}
