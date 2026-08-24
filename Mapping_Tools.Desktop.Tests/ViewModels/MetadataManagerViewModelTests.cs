using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.MetadataManager;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class MetadataManagerViewModelTests
{
    [TestMethod]
    public async Task BrowseExportCommand_WithMultipleFiles_JoinsPathsAndRequestsMultiSelect()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        var viewModel = CreateViewModel(filePicker: picker);

        // Act
        await ExecuteAsync(viewModel.BrowseExportCommand);

        // Assert
        viewModel.ExportPath.Should().Be("first.osu|second.osu");
        viewModel.ExportMapCountText.Should().Be("(2) maps total");
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
    }

    [TestMethod]
    public async Task ImportCommand_WithExistingExportPath_PreservesExportPath()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ImportPath = "source.osu";
        viewModel.ExportPath = "existing-target.osu";

        // Act
        await ExecuteAsync(viewModel.ImportCommand);

        // Assert
        viewModel.ImportPath.Should().Be("source.osu");
        viewModel.ExportPath.Should().Be("existing-target.osu");
    }

    [TestMethod]
    public async Task RunCommand_WithConfiguredMetadata_ExecutesServiceAndResetsProgress()
    {
        // Arrange
        RecordingMetadataManagerService metadataManager = new();
        var viewModel = CreateViewModel(metadataManager);
        viewModel.ExportPath = "first.osu|second.osu";
        viewModel.Artist = "Wave Artist";
        viewModel.RomanisedArtist = "Wave Artist";
        viewModel.Title = "Wave Title";
        viewModel.RomanisedTitle = "Wave Title";
        viewModel.BeatmapCreator = "Mapper";

        // Act
        await ExecuteAsync(viewModel.RunCommand);

        // Assert
        metadataManager.Options.Should().NotBeNull();
        metadataManager.Options!.ExportPath.Should().Be("first.osu|second.osu");
        metadataManager.Options.Artist.Should().Be("Wave Artist");
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    private static MetadataManagerViewModel CreateViewModel(
        RecordingMetadataManagerService? metadataManager = null,
        TestFilePicker? filePicker = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new MetadataManagerViewModel(
            metadataManager ?? new RecordingMetadataManagerService(),
            execution,
            filePicker ?? new TestFilePicker(),
            new RecordingCurrentBeatmapLocator(),
            notifications,
            new TestApplicationDirectories());
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

    private sealed class RecordingMetadataManagerService : IMetadataManagerService
    {
        public MetadataManagerOptions? Options { get; private set; }

        public Task<MetadataManagerOptions> ImportAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MetadataManagerOptions { ImportPath = path });
        }

        public Task<MetadataManagerResult> ExportAsync(
            MetadataManagerOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            progress?.Report(100);
            string[] paths = options.ExportPath.Split('|', StringSplitOptions.RemoveEmptyEntries);
            return Task.FromResult(new MetadataManagerResult(paths));
        }
    }

}
