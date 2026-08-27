using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Timeline;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.AutoFailDetector.ViewModels;

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
        var viewModel = CreateViewModel(service, workspace);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Options!.Path.Should().Be("selected.osu");
        viewModel.ResultSummary.Should().Be(
            "1 unloading objects detected and 2 potential unloading objects detected!");
        viewModel.Markers.Should().ContainSingle(marker => marker.Time == 1000);
        viewModel.Markers[0].Kind.Should().Be(TimelineMarkerKind.Removed);
    }

    [TestMethod]
    public async Task HostedService_WhenStarted_RegistersAlwaysQuickRunAgainstCurrentMap()
    {
        // Arrange
        RecordingAutoFailService service = new();
        QuickRunCommandRegistry registry = new();
        var viewModel = CreateViewModel(
            service,
            currentPath: "current.osu");
        MappingToolQuickRunRegistration registration = new(
            AutoFailDetectorToolDefinition.Definition,
            viewModel.RunQuickAsync);
        MappingToolQuickRunHostedService hosted = new(
            registry,
            [registration],
            new ImmediateTestDispatcher());

        // Act
        await hosted.StartAsync(CancellationToken.None);
        var command = registry.Commands.Single();
        await command.Execute(CancellationToken.None);

        // Assert
        command.DisplayName.Should().Be("Auto-fail Detector");
        command.Targets.Should().Be(QuickRunTargets.Always);
        service.Options!.Path.Should().Be("current.osu");
    }

    [TestMethod]
    public async Task RunCommand_WithFixModeAndNoPotentialObjects_StillRequestsFixPlans()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            Analysis = new AutoFailAnalysis(false, [], [], []),
        };
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = CreateViewModel(service, workspace);
        viewModel.GetAutoFailFix = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.FixPlanRequestCount.Should().Be(1);
        viewModel.Progress.Should().Be(0);
    }

    private static AutoFailDetectorViewModel CreateViewModel(
        RecordingAutoFailService service,
        TestBeatmapWorkspace? workspace = null,
        string? currentPath = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new AutoFailDetectorViewModel(
            service,
            execution,
            workspace ?? new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator(currentPath),
            new ApplicationSettings(),
            new TestDialogService(),
            new RecordingPlatformLauncher());
    }

    private sealed class RecordingAutoFailService : IAutoFailService
    {
        public AutoFailServiceOptions? Options { get; private set; }

        public AutoFailAnalysis Analysis { get; init; } =
            new(true, [1000], [1000, 2000], [1500]);

        public int FixPlanRequestCount { get; private set; }

        public Task<AutoFailRun> AnalyzeAsync(
            AutoFailServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new AutoFailRun(
                Analysis,
                5000));
        }

        public IEnumerable<AutoFailFixPlan> GetFixPlans(
            AutoFailRun run,
            CancellationToken cancellationToken = default)
        {
            FixPlanRequestCount++;
            return [];
        }

        public Task ApplyFixAsync(
            AutoFailRun run,
            AutoFailFixPlan plan,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

}
