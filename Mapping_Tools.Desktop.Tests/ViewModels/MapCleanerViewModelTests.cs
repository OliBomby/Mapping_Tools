using System.Globalization;
using Avalonia.Data;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Timeline;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class MapCleanerViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithWorkspaceSelection_PreservesLegacySummaryAndTimelineKinds()
    {
        // Arrange
        RecordingCleaner cleaner = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["map.osu"]);
        var viewModel = Create(cleaner, workspace);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        cleaner.Paths.Should().Equal("map.osu");
        viewModel.ResultSummary.Should().Be("Successfully removed 16 greenlines and resnapped 20 objects!");
        viewModel.Markers.Should().HaveCount(3);
        viewModel.Markers.Select(marker => marker.Kind)
            .Should().Equal(
                TimelineMarkerKind.Added,
                TimelineMarkerKind.Changed,
                TimelineMarkerKind.Removed);
        viewModel.Progress.Should().Be(0);
        viewModel.HasRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task RunCommand_WithAutoReloadEnabled_DoesNotReloadOrdinaryRun()
    {
        // Arrange
        RecordingCleaner cleaner = new();
        RecordingEditorReloadService reload = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["map.osu"]);
        var viewModel = Create(
            cleaner,
            workspace,
            reload: reload,
            autoReload: true);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        reload.ReloadCount.Should().Be(0);
        viewModel.ResultSummary.Should().StartWith("Successfully removed");
    }

    [TestMethod]
    public async Task RunCommand_WithMultipleMaps_DoesNotExposeEmptyTimeline()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["first.osu", "second.osu"]);
        var viewModel = Create(new RecordingCleaner(), workspace);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasRun.Should().BeFalse();
        viewModel.Markers.Should().BeEmpty();
    }

    [TestMethod]
    public void BeatDivisorArrayToStringConverter_ValidFractionsAndNumbers_ReturnsTypedDivisors()
    {
        // Arrange
        BeatDivisorArrayToStringConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            "1/16, 0.25",
            typeof(IBeatDivisor[]),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().BeOfType<IBeatDivisor[]>()
            .Which.Should().HaveCount(2);
        ((IBeatDivisor[])converted)[0].Should().BeOfType<RationalBeatDivisor>();
        ((IBeatDivisor[])converted)[1].Should().BeOfType<IrrationalBeatDivisor>();
    }

    [TestMethod]
    public void BeatDivisorArrayToStringConverter_TypedDivisors_ReturnsLegacyInvariantText()
    {
        // Arrange
        BeatDivisorArrayToStringConverter converter = new();
        IBeatDivisor[] divisors =
        [
            new RationalBeatDivisor(1, 16),
            new IrrationalBeatDivisor(0.25),
        ];

        // Act
        object converted = converter.Convert(
            divisors,
            typeof(string),
            null,
            new CultureInfo("nl-NL"));

        // Assert
        converted.Should().Be("1/16, 0.25");
    }

    [TestMethod]
    public void BeatDivisorArrayToStringConverter_InvalidEntry_ReturnsValidationError()
    {
        // Arrange
        BeatDivisorArrayToStringConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            "1/16, nope",
            typeof(IBeatDivisor[]),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        var notification = converted.Should()
            .BeOfType<BindingNotification>()
            .Which;
        notification.ErrorType.Should().Be(BindingErrorType.DataValidationError);
        notification.Error.Should().NotBeNull();
        notification.Error!.Message.Should().Be(
            "Beat divisor 'nope' is not a valid fraction or number.");
    }

    [TestMethod]
    public void BeatDivisorArrayToStringConverter_EmptyInput_ReturnsValidationError()
    {
        // Arrange
        BeatDivisorArrayToStringConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            string.Empty,
            typeof(IBeatDivisor[]),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().BeOfType<BindingNotification>()
            .Which.ErrorType.Should().Be(BindingErrorType.DataValidationError);
    }

    [TestMethod]
    public void BeatDivisorArrayToStringConverter_ZeroDivisor_ReturnsValidationError()
    {
        // Arrange
        BeatDivisorArrayToStringConverter converter = new();

        // Act
        object converted = converter.ConvertBack(
            "0",
            typeof(IBeatDivisor[]),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        converted.Should().BeOfType<BindingNotification>()
            .Which.ErrorType.Should().Be(BindingErrorType.DataValidationError);
    }

    [TestMethod]
    public async Task QuickRunHostedService_WhenStarted_UsesCurrentBeatmapAndAlwaysTarget()
    {
        // Arrange
        RecordingCleaner cleaner = new();
        QuickRunCommandRegistry registry = new();
        var viewModel = Create(cleaner, currentPath: "current.osu");
        MappingToolQuickRunRegistration registration = new(
            MappingToolDefinitions.MapCleaner,
            viewModel.RunQuickAsync);
        ImmediateTestDispatcher dispatcher = new();
        MappingToolQuickRunHostedService hosted = new(
            registry,
            [registration],
            dispatcher);

        // Act
        await hosted.StartAsync(CancellationToken.None);
        var command = registry.Commands.Single();
        await command.Execute(CancellationToken.None);

        // Assert
        command.DisplayName.Should().Be("Map Cleaner");
        command.Targets.Should().Be(QuickRunTargets.Always);
        cleaner.Paths.Should().Equal("current.osu");
        dispatcher.PostCount.Should().Be(1);
    }

    private static MapCleanerViewModel Create(
        RecordingCleaner cleaner,
        TestBeatmapWorkspace? workspace = null,
        string? currentPath = null,
        RecordingEditorReloadService? reload = null,
        bool autoReload = false)
    {
        UserNotificationService notifications = new();
        ApplicationSettings settings = new() { AutoReload = autoReload };
        return new MapCleanerViewModel(
            cleaner,
            new ToolExecutionService(notifications, reload ?? new RecordingEditorReloadService(), settings, TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator(currentPath),
            settings,
            new RecordingPlatformLauncher());
    }

    private sealed class RecordingCleaner : IMapCleanerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public Task<MapCleanerResult> CleanAsync(IReadOnlyList<string> paths, MapCleanerOptions options, IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths;
            progress?.Report(100);
            return Task.FromResult(new MapCleanerResult(20, 0, 16, [1000], [2000], [3000], 5000));
        }
    }

}
