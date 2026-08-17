using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mapping_Tools.Core.Tools.SliderCompletionator;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class SliderCompletionatorViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithEverythingMode_PassesProjectValuesAndWorkspacePaths()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        SliderCompletionatorViewModel viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = SliderCompletionatorImportMode.Everything;
        viewModel.FreeVariableSetting = SliderCompletionatorFreeVariable.Length;
        viewModel.Duration = 1.5;
        viewModel.MoveAnchors = true;

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
        service.Options.Should().NotBeNull();
        service.Options!.ImportModeSetting.Should().Be(SliderCompletionatorImportMode.Everything);
        service.Options.FreeVariableSetting.Should().Be(SliderCompletionatorFreeVariable.Length);
        service.Options.Duration.Should().Be(1.5);
        service.Options.MoveAnchors.Should().BeTrue();
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_UsesQuickReloadAndSelectedMode()
    {
        // Arrange
        RecordingCompletionator service = new();
        SliderCompletionatorViewModel viewModel = Create(
            service,
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator("current.osu"));

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.Paths.Should().Equal("current.osu");
        service.Options!.ImportModeSetting.Should().Be(SliderCompletionatorImportMode.Selected);
    }

    [TestMethod]
    public async Task RunCommand_WithAlwaysQuickRunAndNonSelectedMode_UsesWorkspacePaths()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        ApplicationSettings settings = new() { AlwaysQuickRun = true };
        SliderCompletionatorViewModel viewModel = Create(
            service,
            workspace,
            settings: settings);
        viewModel.ImportModeSetting = SliderCompletionatorImportMode.Everything;

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
    }

    [TestMethod]
    public async Task RunCommand_WithNonFiniteDuration_DoesNotInvokeService()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        SliderCompletionatorViewModel viewModel = Create(service, workspace);
        viewModel.Duration = double.PositiveInfinity;

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.Paths.Should().BeNull();
        viewModel.HasErrors.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void Visibility_WhenEndTimeIsEnabled_HidesDurationAndShowsEndTime()
    {
        // Arrange
        SliderCompletionatorViewModel viewModel = Create(new RecordingCompletionator());

        // Act
        viewModel.UseEndTime = true;

        // Assert
        viewModel.DurationVisible.Should().BeFalse();
        viewModel.EndTimeVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Visibility_WhenCurrentEditorTimeAndLengthAreSelected_HidesEndTimeAndLength()
    {
        // Arrange
        SliderCompletionatorViewModel viewModel = Create(new RecordingCompletionator());
        viewModel.UseEndTime = true;

        // Act
        viewModel.UseCurrentEditorTime = true;
        viewModel.FreeVariableSetting = SliderCompletionatorFreeVariable.Length;

        // Assert
        viewModel.EndTimeVisible.Should().BeFalse();
        viewModel.LengthVisible.Should().BeFalse();
        viewModel.VelocityVisible.Should().BeTrue();
    }

    private static SliderCompletionatorViewModel Create(
        RecordingCompletionator service,
        TestBeatmapWorkspace? workspace = null,
        RecordingCurrentBeatmapLocator? currentBeatmap = null,
        ApplicationSettings? settings = null)
    {
        return new SliderCompletionatorViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            settings ?? new ApplicationSettings());
    }

    private sealed class RecordingCompletionator : ISliderCompletionatorService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public SliderCompletionatorOptions? Options { get; private set; }

        public Task<SliderCompletionatorResult> CompleteAsync(
            IReadOnlyList<string> paths,
            SliderCompletionatorOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            progress?.Report(100);
            return Task.FromResult(new SliderCompletionatorResult(paths, 2));
        }
    }

    private sealed class RecordingCurrentBeatmapLocator(string? path) : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(path);
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
