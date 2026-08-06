using CommunityToolkit.Mvvm.Input;
using Avalonia.Data;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

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
        MapCleanerViewModel viewModel = Create(cleaner, workspace: workspace);

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        cleaner.Paths.Should().Equal("map.osu");
        viewModel.ResultSummary.Should().Be("Successfully removed 16 greenlines and resnapped 20 objects!");
        viewModel.Markers.Should().HaveCount(3);
        viewModel.Progress.Should().Be(100);
        viewModel.HasRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task RunCommand_WithAutoReloadEnabled_DoesNotReloadOrdinaryRun()
    {
        // Arrange
        RecordingCleaner cleaner = new();
        RecordingReload reload = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["map.osu"]);
        MapCleanerViewModel viewModel = Create(
            cleaner,
            workspace: workspace,
            reload: reload,
            autoReload: true);

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        reload.ReloadCount.Should().Be(0);
        viewModel.ResultSummary.Should().StartWith("Successfully removed");
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
            System.Globalization.CultureInfo.InvariantCulture);

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
            new IrrationalBeatDivisor(0.25)
        ];

        // Act
        object converted = converter.Convert(
            divisors,
            typeof(string),
            null,
            new System.Globalization.CultureInfo("nl-NL"));

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
            System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        converted.Should().BeOfType<BindingNotification>()
            .Which.ErrorType.Should().Be(BindingErrorType.DataValidationError);
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
            System.Globalization.CultureInfo.InvariantCulture);

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
            System.Globalization.CultureInfo.InvariantCulture);

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
        MapCleanerViewModel viewModel = Create(cleaner, registry: registry, currentPath: "current.osu");
        MappingToolQuickRunRegistration registration = new(
            "map-cleaner",
            "Map Cleaner",
            QuickRunTargets.Always,
            viewModel.RunQuickAsync);
        MappingToolQuickRunHostedService hosted = new(registry, [registration]);

        // Act
        await hosted.StartAsync(CancellationToken.None);
        QuickRunCommand command = registry.Commands.Single();
        await command.Execute(CancellationToken.None);

        // Assert
        command.DisplayName.Should().Be("Map Cleaner");
        command.Targets.Should().Be(QuickRunTargets.Always);
        cleaner.Paths.Should().Equal("current.osu");
    }

    private static MapCleanerViewModel Create(
        RecordingCleaner cleaner,
        TestBeatmapWorkspace? workspace = null,
        QuickRunCommandRegistry? registry = null,
        string? currentPath = null,
        RecordingReload? reload = null,
        bool autoReload = false)
    {
        UserNotificationService notifications = new();
        ApplicationSettings settings = new() { AutoReload = autoReload };
        return new MapCleanerViewModel(
            cleaner,
            new ToolExecutionService(notifications, reload ?? new RecordingReload(), settings, TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace(),
            new StubLocator(currentPath),
            settings,
            registry ?? new QuickRunCommandRegistry(),
            new StubProjects(),
            new TestDialogService(),
            notifications,
            new StubLauncher());
    }

    private sealed class RecordingCleaner : IMapCleanerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }
        public Task<MapCleanerResult> CleanAsync(IReadOnlyList<string> paths, MapCleanerOptions options, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            Paths = paths;
            progress?.Report(100);
            return Task.FromResult(new MapCleanerResult(20, 0, 16, [1000], [2000], [3000], 5000));
        }
    }

    private sealed class StubLocator(string? path) : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default) => Task.FromResult(path);
    }

    private sealed class RecordingReload : IEditorReloadService
    {
        public int ReloadCount { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            ReloadCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubLauncher : IPlatformLauncher
    {
        public Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubProjects : IProjectService
    {
        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition) => Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);
        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition) => Path.GetTempPath();
        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition) => definition.CreateProject();
        public Task SaveAsync<TProject>(string path, TProject project, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<TProject> LoadAsync<TProject>(string path, CancellationToken cancellationToken = default) => Task.FromException<TProject>(new FileNotFoundException());
        public Task AutoSaveAsync<TProject>(ProjectDefinition<TProject> definition, TProject project, IEnumerable<string>? additionalPaths = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string?> SaveAsAsync<TProject>(ProjectDefinition<TProject> definition, TProject project, string? suggestedFileName = null, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(ProjectDefinition<TProject> definition, CancellationToken cancellationToken = default) => Task.FromResult<ProjectOpenResult<TProject>?>(null);
    }
}
