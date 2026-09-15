using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
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

    [TestMethod]
    public async Task RunCommand_WithFixGuideEnabled_ShowsLegacyFixGuideDialog()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            FixPlans = [new AutoFailFixPlan([1], "Auto-fail fix guide. Place these extra objects to fix auto-fail:")],
        };
        TestDialogService dialogs = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = CreateViewModel(service, workspace, dialogs: dialogs);
        viewModel.GetAutoFailFix = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().Be(1);
        dialogs.LastMessageTitle.Should().Be("Solution 1");
        dialogs.LastMessage.Should().Contain("Auto-fail fix guide");
        dialogs.LastMessage.Should().Contain("Do you want to use this solution?");
        dialogs.LastMessageChoiceLabels.Should().Equal("Yes", "No", "Cancel");
        service.ApplyFixRequestCount.Should().Be(0);
    }

    [TestMethod]
    public async Task RunCommand_WithAutoInsertEnabled_AppliesAcceptedFixPlan()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            FixPlans = [new AutoFailFixPlan([1], "Auto-fail fix guide")],
        };
        TestDialogService dialogs = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        RecordingEditorReloadService reload = new();
        var viewModel = CreateViewModel(service, workspace, dialogs: dialogs, reload: reload);
        viewModel.GetAutoFailFix = true;
        viewModel.AutoPlaceFix = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.ApplyFixRequestCount.Should().Be(1);
        viewModel.ResultSummary.Should().EndWith(" Fix applied.");
        reload.ReloadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task RunQuickAsync_WithAutoInsertEnabled_ReloadsAfterAcceptedFixPlan()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            FixPlans = [new AutoFailFixPlan([1], "Auto-fail fix guide")],
        };
        TestDialogService dialogs = new();
        RecordingEditorReloadService reload = new();
        var viewModel = CreateViewModel(
            service,
            currentPath: "current.osu",
            dialogs: dialogs,
            reload: reload);
        viewModel.GetAutoFailFix = true;
        viewModel.AutoPlaceFix = true;

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.ApplyFixRequestCount.Should().Be(1);
        reload.ReloadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task RunCommand_WhenFixPlanningFails_ShowsFixPlanningErrorDialog()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            FixPlanException = new InvalidOperationException("No fix solution."),
        };
        TestDialogService dialogs = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = CreateViewModel(service, workspace, dialogs: dialogs);
        viewModel.GetAutoFailFix = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().Be(1);
        dialogs.LastMessageTitle.Should().Be("Auto-fail fix");
        dialogs.LastMessage.Should().Be("Could not create an auto-fail fix guide.");
    }

    private static AutoFailDetectorViewModel CreateViewModel(
        RecordingAutoFailService service,
        TestBeatmapWorkspace? workspace = null,
        string? currentPath = null,
        TestDialogService? dialogs = null,
        RecordingEditorReloadService? reload = null)
    {
        UserNotificationService notifications = new();
        reload ??= new RecordingEditorReloadService();
        ToolExecutionService execution = new(
            notifications,
            reload,
            new DesktopApplicationSettings(),
            TimeProvider.System);
        TestBeatmapWorkspace effectiveWorkspace = workspace ?? new TestBeatmapWorkspace();
        effectiveWorkspace.QuickRunPath = currentPath;
        return new AutoFailDetectorViewModel(
            service,
            execution,
            effectiveWorkspace,
            new DesktopApplicationSettings(),
            dialogs ?? new TestDialogService(),
            new RecordingPlatformLauncher());
    }

    private sealed class RecordingAutoFailService : IAutoFailService
    {
        public AutoFailServiceOptions? Options { get; private set; }

        public AutoFailAnalysis Analysis { get; init; } =
            new(true, [1000], [1000, 2000], [1500]);

        public int FixPlanRequestCount { get; private set; }

        public IReadOnlyList<AutoFailFixPlan> FixPlans { get; init; } = [];

        public Exception? FixPlanException { get; init; }

        public int ApplyFixRequestCount { get; private set; }

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
            if (FixPlanException is not null) throw FixPlanException;

            return FixPlans;
        }

        public Task ApplyFixAsync(
            AutoFailRun run,
            AutoFailFixPlan plan,
            CancellationToken cancellationToken = default)
        {
            ApplyFixRequestCount++;
            return Task.CompletedTask;
        }
    }

}
