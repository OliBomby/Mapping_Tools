using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.MetadataManager;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.MetadataManager;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class MetadataManagerViewModelTests
{
    [TestMethod]
    public async Task BrowseExportCommand_WithMultipleFiles_JoinsPathsAndRequestsMultiSelect()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        MetadataManagerViewModel viewModel = CreateViewModel(filePicker: picker);

        // Act
        await ExecuteAsync(viewModel.BrowseExportCommand);

        // Assert
        viewModel.ExportPath.Should().Be("first.osu|second.osu");
        viewModel.ExportMapCountText.Should().Be("(2) maps total");
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
    }

    [TestMethod]
    public async Task RunCommand_WithConfiguredMetadata_ExecutesServiceAndResetsProgress()
    {
        // Arrange
        RecordingMetadataManagerService metadataManager = new();
        MetadataManagerViewModel viewModel = CreateViewModel(metadataManager: metadataManager);
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

    [TestMethod]
    public async Task NewProjectAsync_WithModifiedInputs_InstallsDefaultExportPath()
    {
        // Arrange
        MetadataManagerViewModel viewModel = CreateViewModel();
        viewModel.ExportPath = "modified.osu";

        // Act
        await viewModel.NewProjectAsync();

        // Assert
        viewModel.ExportPath.Should().EndWith("metadata_manager.osu");
    }

    private static MetadataManagerViewModel CreateViewModel(
        RecordingMetadataManagerService? metadataManager = null,
        TestFilePicker? filePicker = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new StubReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new MetadataManagerViewModel(
            metadataManager ?? new RecordingMetadataManagerService(),
            execution,
            filePicker ?? new TestFilePicker(),
            new StubCurrentBeatmapLocator(),
            new StubProjectService(),
            notifications,
            new TestApplicationDirectories());
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command) => command.ExecuteAsync(null);

    private sealed class RecordingMetadataManagerService : IMetadataManagerService
    {
        public MetadataManagerOptions? Options { get; private set; }

        public Task<MetadataManagerOptions> ImportAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new MetadataManagerOptions { ImportPath = path });

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

    private sealed class StubCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubProjectService : IProjectService
    {
        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition) =>
            Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition) =>
            Path.GetTempPath();

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition) =>
            definition.CreateProject();

        public Task SaveAsync<TProject>(
            string path,
            TProject project,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<TProject> LoadAsync<TProject>(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromException<TProject>(new FileNotFoundException());

        public Task AutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string?> SaveAsAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProjectOpenResult<TProject>?>(null);
    }
}
