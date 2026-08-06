using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class RhythmGuideViewModelTests
{
    [TestMethod]
    public async Task BrowseSourcesCommand_WithMultipleFiles_PreservesPickerOrderAndCount()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        RhythmGuideViewModel viewModel = CreateViewModel(filePicker: picker);

        // Act
        await ExecuteAsync(viewModel.BrowseSourcesCommand);

        // Assert
        viewModel.SourcePathsText.Should().Be("first.osu|second.osu");
        viewModel.SourceCount.Should().Be(2);
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
    }

    [TestMethod]
    public async Task RunCommand_WithNewMap_ExecutesUseCaseAndRevealsOutput()
    {
        // Arrange
        RecordingRhythmGuideService rhythmGuide = new();
        TestFileRevealService reveal = new();
        RhythmGuideViewModel viewModel = CreateViewModel(
            rhythmGuide: rhythmGuide,
            fileReveal: reveal);
        viewModel.SourcePathsText = "source.osu";
        viewModel.ExportPath = "guide.osu";

        // Act
        await ExecuteAsync(viewModel.RunCommand);

        // Assert
        rhythmGuide.Options.Should().NotBeNull();
        rhythmGuide.Options!.Paths.Should().Equal("source.osu");
        reveal.RevealedPaths.Should().Equal("guide.osu");
        viewModel.Progress.Should().Be(100);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task NewProjectAsync_WithDirtyStateAndRejectedConfirmation_PreservesInputs()
    {
        // Arrange
        TestDialogService dialogs = new() { BooleanResult = false };
        RhythmGuideViewModel viewModel = CreateViewModel(dialogs: dialogs);
        viewModel.OutputName = "Unsaved";

        // Act
        await viewModel.NewProjectAsync();

        // Assert
        viewModel.OutputName.Should().Be("Unsaved");
        viewModel.IsDirty.Should().BeTrue();
        dialogs.MessageCount.Should().Be(1);
    }

    private static RhythmGuideViewModel CreateViewModel(
        RecordingRhythmGuideService? rhythmGuide = null,
        TestFilePicker? filePicker = null,
        TestFileRevealService? fileReveal = null,
        TestDialogService? dialogs = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new StubReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new RhythmGuideViewModel(
            rhythmGuide ?? new RecordingRhythmGuideService(),
            execution,
            filePicker ?? new TestFilePicker(),
            new StubCurrentBeatmapLocator(),
            new StubProjectService(),
            dialogs ?? new TestDialogService(),
            fileReveal ?? new TestFileRevealService(),
            new StubRhythmGuideWindowService(),
            notifications,
            new TestApplicationDirectories());
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command) => command.ExecuteAsync(null);

    private sealed class RecordingRhythmGuideService : IRhythmGuideService
    {
        public RhythmGuideOptions? Options { get; private set; }

        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new RhythmGuideResult(
                options.ExportPath,
                12,
                options.ExportMode));
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

    private sealed class StubRhythmGuideWindowService : IRhythmGuideWindowService
    {
        public void Show(RhythmGuideViewModel viewModel)
        {
        }
    }

    private sealed class StubProjectService : IProjectService
    {
        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition) =>
            Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition) =>
            Path.GetTempPath();

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition) =>
            definition.CreateProject();

        public Task SaveAsync<TProject>(string path, TProject project, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<TProject> LoadAsync<TProject>(string path, CancellationToken cancellationToken = default) =>
            Task.FromException<TProject>(new FileNotFoundException());

        public Task AutoSaveAsync<TProject>(ProjectDefinition<TProject> definition, TProject project, IEnumerable<string>? additionalPaths = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string?> SaveAsAsync<TProject>(ProjectDefinition<TProject> definition, TProject project, string? suggestedFileName = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(ProjectDefinition<TProject> definition, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProjectOpenResult<TProject>?>(null);
    }
}
