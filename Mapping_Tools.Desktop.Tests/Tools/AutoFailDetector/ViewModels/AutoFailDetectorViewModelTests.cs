using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.AutoFailDetector.ViewModels;

[TestClass]
public sealed class AutoFailDetectorViewModelTests
{
    [TestMethod]
    public void Install_WithSerializedSnapshot_RestoresEveryOption()
    {
        // Arrange
        IShellProjectFeature<AutoFailDetectorProject> source = CreateViewModel(new RecordingAutoFailService());
        AutoFailDetectorProject options = new()
        {
            ShowUnloadingObjects = false,
            ShowPotentialUnloadingObjects = true,
            ShowPotentialDisruptors = true,
            ApproachRateOverride = 12.5,
            OverallDifficultyOverride = 8.5,
            PhysicsUpdateLeniency = 17,
            GetAutoFailFix = true,
            AutoPlaceFix = true,
        };
        source.Install(options);
        VersionedProjectJsonSerializer serializer = new();
        string json = serializer.Serialize(source.ProjectDefinition.ConfigSchema, source.Snapshot());
        IShellProjectFeature<AutoFailDetectorProject> target = CreateViewModel(new RecordingAutoFailService());

        // Act
        target.Install(serializer.Deserialize<AutoFailDetectorProject>(target.ProjectDefinition.ConfigSchema, json));

        // Assert
        target.Snapshot().Should().BeEquivalentTo(options);
        json.Should().NotContain("Markers").And.NotContain("HasRun").And.NotContain("EndTime");
    }

    [TestMethod]
    public void Install_WithNewProject_RestoresFormDefaults()
    {
        // Arrange
        var viewModel = CreateViewModel(new RecordingAutoFailService());
        IShellProjectFeature<AutoFailDetectorProject> feature = viewModel;
        var defaults = feature.Snapshot();
        viewModel.ShowUnloadingObjects = false;
        viewModel.ShowPotentialUnloadingObjects = true;
        viewModel.ShowPotentialDisruptors = true;
        viewModel.ApproachRateOverride = 12;
        viewModel.OverallDifficultyOverride = 8;
        viewModel.PhysicsUpdateLeniency = 20;
        viewModel.GetAutoFailFix = true;
        viewModel.AutoPlaceFix = true;

        // Act
        feature.Install(feature.ProjectDefinition.CreateProject());

        // Assert
        feature.Snapshot().Should().BeEquivalentTo(defaults);
    }

    [TestMethod]
    [DataRow(nameof(AutoFailDetectorViewModel.ShowUnloadingObjects), false)]
    [DataRow(nameof(AutoFailDetectorViewModel.ShowPotentialUnloadingObjects), true)]
    [DataRow(nameof(AutoFailDetectorViewModel.ShowPotentialDisruptors), true)]
    [DataRow(nameof(AutoFailDetectorViewModel.ApproachRateOverride), 12.5)]
    [DataRow(nameof(AutoFailDetectorViewModel.OverallDifficultyOverride), 8.5)]
    [DataRow(nameof(AutoFailDetectorViewModel.PhysicsUpdateLeniency), 17)]
    [DataRow(nameof(AutoFailDetectorViewModel.GetAutoFailFix), true)]
    [DataRow(nameof(AutoFailDetectorViewModel.AutoPlaceFix), true)]
    public void Undo_WithChangedOption_RestoresOriginalAndAllowsRedo(string propertyName, object value)
    {
        // Arrange
        var viewModel = CreateViewModel(new RecordingAutoFailService());
        var property = typeof(AutoFailDetectorViewModel).GetProperty(propertyName)!;
        object? original = property.GetValue(viewModel);
        ProjectUndoHistory<AutoFailDetectorProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        property.SetValue(viewModel, value);

        // Act
        history.Undo();

        // Assert
        property.GetValue(viewModel).Should().Be(original);
        history.CanUndo.Should().BeFalse();
        history.CanRedo.Should().BeTrue();
        history.Redo();
        property.GetValue(viewModel).Should().Be(value);
    }

    [TestMethod]
    public async Task RunCommand_WithUndoHistory_DoesNotRecordAnalysisResults()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = CreateViewModel(new RecordingAutoFailService(), workspace);
        ProjectUndoHistory<AutoFailDetectorProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);
        history.Capture();

        // Assert
        viewModel.HasRun.Should().BeTrue();
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunCommand_WithWorkspaceMap_PublishesSuccessAndInstallsFilteredMarkers()
    {
        // Arrange
        RecordingAutoFailService service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(service, workspace, notifications: notifications);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Options!.Path.Should().Be("selected.osu");
        published.Where(notification =>
                notification.Severity == UserNotificationSeverity.Success
                && notification.Message == "1 unloading objects detected and 2 potential unloading objects detected!")
            .Should().ContainSingle();
        viewModel.Markers.Should().ContainSingle(marker => Precision.AlmostEquals(marker.Time, 1000));
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
        var viewModel = CreateViewModel(
            service,
            workspace,
            dialogs: dialogs,
            notifications: new UserNotificationService());
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
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(
            service,
            workspace,
            dialogs: dialogs,
            notifications: notifications);
        viewModel.GetAutoFailFix = true;
        viewModel.AutoPlaceFix = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.ApplyFixRequestCount.Should().Be(1);
        published.Where(notification =>
                notification.Severity == UserNotificationSeverity.Success
                && notification.Message == "Applied the auto-fail fix.")
            .Should().ContainSingle();
        service.QuickRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithAutoInsertEnabled_PassesQuickRunToService()
    {
        // Arrange
        RecordingAutoFailService service = new()
        {
            FixPlans = [new AutoFailFixPlan([1], "Auto-fail fix guide")],
        };
        TestDialogService dialogs = new();
        var viewModel = CreateViewModel(
            service,
            currentPath: "current.osu",
            dialogs: dialogs);
        viewModel.GetAutoFailFix = true;
        viewModel.AutoPlaceFix = true;

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.ApplyFixRequestCount.Should().Be(1);
        service.QuickRun.Should().BeTrue();
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
        IDialogService? dialogs = null,
        IUserNotificationService? notifications = null)
    {
        var notificationService = notifications ?? new UserNotificationService();
        ToolExecutionService execution = new(
            notificationService,
            TimeProvider.System);
        var effectiveWorkspace = workspace ?? new TestBeatmapWorkspace();
        effectiveWorkspace.QuickRunPath = currentPath;
        return new AutoFailDetectorViewModel(
            service,
            execution,
            effectiveWorkspace,
            new DesktopApplicationSettings(),
            dialogs ?? new TestDialogService(),
            new RecordingPlatformLauncher(),
            notificationService);
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

        public bool QuickRun { get; private set; }

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
            bool quickRun = false,
            CancellationToken cancellationToken = default)
        {
            ApplyFixRequestCount++;
            QuickRun = quickRun;
            return Task.CompletedTask;
        }
    }
}
