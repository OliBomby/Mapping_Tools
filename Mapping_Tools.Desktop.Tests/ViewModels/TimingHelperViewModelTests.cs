using System.Globalization;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.TimingHelper;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class TimingHelperViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithWorkspaceSelection_PassesTimingOptionsAndResetsProgress()
    {
        // Arrange
        RecordingTimingHelper service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.Objects = false;
        viewModel.Leniency = 10;
        viewModel.BeatsBetween = 1;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("selected.osu");
        service.Options.Should().NotBeNull();
        service.Options!.Objects.Should().BeFalse();
        service.Options.Leniency.Should().Be(10);
        service.Options.BeatsBetween.Should().Be(1);
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_UsesQuickPathAndReloadsEditor()
    {
        // Arrange
        RecordingTimingHelper service = new();
        RecordingCurrentBeatmapLocator currentBeatmap = new("current.osu");
        RecordingReloadService reload = new();
        var viewModel = Create(
            service,
            new TestBeatmapWorkspace(),
            currentBeatmap,
            reload);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.Paths.Should().Equal("current.osu");
        reload.ReloadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task RunCommand_WithNegativeLeniency_DoesNotInvokeService()
    {
        // Arrange
        RecordingTimingHelper service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.Leniency = -1;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().BeNull();
        viewModel.HasErrors.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunCommand_WithInfiniteLeniency_DoesNotInvokeService()
    {
        // Arrange
        RecordingTimingHelper service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.Leniency = double.PositiveInfinity;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().BeNull();
        viewModel.HasErrors.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task StartAsync_WithTimingHelperRegistration_RegistersAlwaysTargetAndRunsCurrentBeatmap()
    {
        // Arrange
        RecordingTimingHelper service = new();
        QuickRunCommandRegistry registry = new();
        var viewModel = Create(
            service,
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator("current.osu"));
        MappingToolQuickRunRegistration registration = new(
            MappingToolDefinitions.TimingHelper,
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
        command.DisplayName.Should().Be("Timing Helper");
        command.Targets.Should().Be(QuickRunTargets.Always);
        service.Paths.Should().Equal("current.osu");
    }

    [TestMethod]
    public void Convert_WithLegacyWysiValue_ReturnsLegacyDisplayText()
    {
        // Arrange
        InvariantDoubleConverter converter = new();

        // Act
        object converted = converter.Convert(
            727d,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().Be("727 WYSI");
    }

    [TestMethod]
    public void ConvertBack_WithFractionalBeatValue_ReturnsExactDouble()
    {
        // Arrange
        InvariantDoubleConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            "0.5",
            typeof(double),
            -1,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().Be(0.5d);
    }

    [TestMethod]
    public void ConvertBack_WithInvalidTextAndFallback_ReturnsFallbackValue()
    {
        // Arrange
        InvariantDoubleConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            "not a number",
            typeof(double),
            -1,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().Be(-1d);
    }

    private static TimingHelperViewModel Create(
        RecordingTimingHelper? service = null,
        TestBeatmapWorkspace? workspace = null,
        RecordingCurrentBeatmapLocator? currentBeatmap = null,
        RecordingReloadService? reload = null)
    {
        UserNotificationService notifications = new();
        return new TimingHelperViewModel(
            service ?? new RecordingTimingHelper(),
            new ToolExecutionService(
                notifications,
                reload ?? new RecordingReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            new ApplicationSettings());
    }

    private sealed class RecordingTimingHelper : ITimingHelperService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public TimingHelperOptions? Options { get; private set; }

        public Task<TimingHelperResult> AdjustAsync(
            IReadOnlyList<string> paths,
            TimingHelperOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            progress?.Report(100);
            return Task.FromResult(new TimingHelperResult(paths, 2));
        }
    }

    private sealed class RecordingCurrentBeatmapLocator(string? path) : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(path);
        }
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        public int ReloadCount { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            ReloadCount++;
            return Task.CompletedTask;
        }
    }
}
