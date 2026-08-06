using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AutoFailDetectorViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithWorkspaceMap_InstallsLegacySummaryAndFilteredMarkers()
    {
        // Arrange
        RecordingAutoFailService service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        AutoFailDetectorViewModel viewModel = CreateViewModel(service, workspace: workspace);

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.Options!.Path.Should().Be("selected.osu");
        viewModel.ResultSummary.Should().Be(
            "1 unloading objects detected and 2 potential unloading objects detected!");
        viewModel.Markers.Should().ContainSingle(marker => marker.Time == 1000);
        viewModel.Markers[0].Kind.Should().Be(Mapping_Tools.Application.Timeline.TimelineMarkerKind.Removed);
    }

    [TestMethod]
    public async Task HostedService_WhenStarted_RegistersAlwaysQuickRunAgainstCurrentMap()
    {
        // Arrange
        RecordingAutoFailService service = new();
        QuickRunCommandRegistry registry = new();
        AutoFailDetectorViewModel viewModel = CreateViewModel(
            service,
            registry: registry,
            currentPath: "current.osu");
        AutoFailQuickRunHostedService hosted = new(registry, viewModel);

        // Act
        await hosted.StartAsync(CancellationToken.None);
        QuickRunCommand command = registry.Commands.Single();
        await command.Execute(CancellationToken.None);

        // Assert
        command.DisplayName.Should().Be("Auto-fail Detector");
        command.Targets.Should().Be(QuickRunTargets.Always);
        service.Options!.Path.Should().Be("current.osu");
    }

    private static AutoFailDetectorViewModel CreateViewModel(
        RecordingAutoFailService service,
        TestBeatmapWorkspace? workspace = null,
        QuickRunCommandRegistry? registry = null,
        string? currentPath = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new StubReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new AutoFailDetectorViewModel(
            service,
            execution,
            workspace ?? new TestBeatmapWorkspace(),
            new StubCurrentBeatmapLocator(currentPath),
            new ApplicationSettings(),
            new TestDialogService(),
            registry ?? new QuickRunCommandRegistry(),
            new StubLauncher());
    }

    private sealed class RecordingAutoFailService : IAutoFailService
    {
        public AutoFailOptions? Options { get; private set; }

        public Task<AutoFailRun> AnalyzeAsync(
            AutoFailOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new AutoFailRun(
                new AutoFailAnalysis(true, [1000], [1000, 2000], [1500]),
                5000));
        }

        public IEnumerable<AutoFailFixPlan> GetFixPlans(
            AutoFailRun run,
            CancellationToken cancellationToken = default) => [];

        public Task ApplyFixAsync(
            AutoFailRun run,
            AutoFailFixPlan plan,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubCurrentBeatmapLocator(string? path) : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(path);
    }

    private sealed class StubReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubLauncher : IPlatformLauncher
    {
        public Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
