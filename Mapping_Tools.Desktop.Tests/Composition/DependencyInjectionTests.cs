using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.MapCleaner.ViewModels;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Interactions;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class DependencyInjectionTests
{
    [TestMethod]
    public void AddMappingToolsDesktop_DefaultRegistration_RegistersExpectedSingletons()
    {
        // Arrange
        ServiceCollection services = new();

        Type[] expectedSingletons =
        [
            typeof(MainWindow),
            typeof(MainViewModel),
            typeof(PreferencesViewModel),
            typeof(RhythmGuideViewModel),
            typeof(HitsoundPreviewHelperViewModel),
            typeof(AutoFailDetectorViewModel),
            typeof(MapCleanerViewModel),
            typeof(ComboColourStudioViewModel),
            typeof(HitsoundStudioViewModel),
            typeof(SlideratorViewModel),
            typeof(TumourGeneratorViewModel),
            typeof(PatternGalleryViewModel),
            typeof(GeometryDashboardViewModel),
            typeof(GeometryDashboardProject),
            typeof(GeometryDashboardServiceOptions),
            typeof(IGeometryDashboardService),
            typeof(GeometryDashboardLifecycleCoordinator),
            typeof(IUpdateService),
            typeof(IDialogService),
            typeof(IFilePicker),
            typeof(IClipboardService),
            typeof(IPlatformLauncher),
            typeof(IFileRevealService),
            typeof(IApplicationThemeService),
            typeof(IRhythmGuideWindowService),
            typeof(IApplicationDirectories),
            typeof(ISettingsStore),
            typeof(ISettingsPathEnvironment),
            typeof(ISettingsPathService),
            typeof(ISettingsService),
            typeof(DesktopApplicationSettings),
            typeof(Mapping_Tools.Application.Settings.Models.ApplicationSettings),
            typeof(TimeProvider),
            typeof(ITextFileStore),
            typeof(IUserNotificationService),
            typeof(IToolExecutionService),
            typeof(IQuickRunCommandRegistry),
            typeof(IQuickRunService),
            typeof(IHotkeyBindingCoordinator),
            typeof(IGlobalHotkeyService),
            typeof(IBeatmapBackupStore),
            typeof(IBeatmapBackupService),
            typeof(IQuickUndoCommandService),
            typeof(ILiveBeatmapReader),
            typeof(IEditorReloadService),
            typeof(IBeatmapEditingGateway),
            typeof(IBeatmapsetFileSystem),
            typeof(ICurrentBeatmapLocator),
            typeof(IBetterSaveService),
            typeof(IRhythmGuideService),
            typeof(IHitsoundPreviewHelperService),
            typeof(IAutoFailService),
            typeof(IMapCleanerService),
            typeof(IComboColourStudioService),
            typeof(ITimingHelperService),
            typeof(ISliderCompletionatorService),
            typeof(ISliderMergerService),
            typeof(IMapCleanerSampleService),
            typeof(IBetterSaveOverrideService),
            typeof(IBeatmapWorkspace),
            typeof(IProjectSerializer),
            typeof(IProjectStore),
            typeof(IProjectService),
        ];

        // Act
        services.AddMappingToolsDesktop();

        // Assert
        foreach (var serviceType in expectedSingletons)
        {
            var registration = services.SingleOrDefault(descriptor => descriptor.ServiceType == serviceType);

            registration.Should().NotBeNull($"{serviceType.Name} is not registered.");
            registration.Lifetime.Should().Be(ServiceLifetime.Singleton, $"{serviceType.Name} has the wrong lifetime.");
        }
    }

    [TestMethod]
    public void BuildServiceProvider_WithDesktopRegistrations_PassesValidation()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();

        // Act
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        // Assert
        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void BuildServiceProvider_WithDesktopAndHostedRegistrations_PassesValidation()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMappingToolsDesktop();
        services.AddMappingToolsHostedServices();

        // Act
        var act = () =>
        {
            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
        };

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void AddMappingToolsHostedServices_DefaultRegistration_RegistersExpectedLifecycles()
    {
        // Arrange
        ServiceCollection services = new();

        services.AddMappingToolsHostedServices();

        // Act
        var hosted = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .ToArray();
        // Assert
        hosted.Length.Should().Be(4);
        hosted.All(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
    }

    [TestMethod]
    public async Task StopAsync_DuringHostShutdown_StopsToolExecution()
    {
        // Arrange
        RecordingToolExecutionService execution = new();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IToolExecutionService>(execution);
        builder.Services.AddHostedService<ToolExecutionHostedService>();
        using var host = builder.Build();

        // Act
        await host.StartAsync();
        await host.StopAsync();

        // Assert
        execution.StopCount.Should().Be(1);
    }

    [TestMethod]
    public async Task BetterSaveOverrideHostedService_StartAndStop_AppliesSettingsAndStopsWatcher()
    {
        // Arrange
        TestBetterSaveOverrideService betterSaveOverride = new();
        DesktopApplicationSettings settings = new()
        {
            SongsPath = @"C:\osu!\Songs",
            OverrideOsuSave = true,
        };
        BetterSaveOverrideHostedService service = new(betterSaveOverride, settings);

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert
        betterSaveOverride.Configurations.Should().Equal((settings.SongsPath, true));
        betterSaveOverride.Stopped.Should().BeTrue();
    }

    [TestMethod]
    public async Task GlobalHotkeyHostedService_StartAndStop_ConnectsBindingsAndStopsListener()
    {
        // Arrange
        RecordingGlobalHotkeyService hotkeys = new();
        RecordingQuickRunService quickRun = new();
        DesktopApplicationSettings settings = new()
        {
            QuickRunHotkey = new HotkeySettings(56, 2),
            QuickUndoHotkey = new HotkeySettings(69, 6),
            BetterSaveHotkey = new HotkeySettings(31, 2),
        };
        RecordingQuickUndoCommandService quickUndo = new();
        TestBetterSaveService betterSave = new();
        GlobalHotkeyHostedService service = new(
            hotkeys,
            quickRun,
            quickUndo,
            betterSave,
            settings);

        // Act
        await service.StartAsync(CancellationToken.None);
        // Assert
        hotkeys.Started.Should().BeTrue();
        hotkeys.Hotkeys["quick-run"].Should().Be(settings.QuickRunHotkey);
        hotkeys.Hotkeys["quick-undo"].Should().Be(settings.QuickUndoHotkey);
        hotkeys.Hotkeys["better-save"].Should().Be(settings.BetterSaveHotkey);

        await hotkeys.Callbacks["quick-run"](CancellationToken.None);
        await hotkeys.Callbacks["quick-undo"](CancellationToken.None);
        await hotkeys.Callbacks["better-save"](CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        quickRun.RunCount.Should().Be(1);
        quickUndo.RunCount.Should().Be(1);
        betterSave.ExecutionCount.Should().Be(1);
        hotkeys.Stopped.Should().BeTrue();
    }

    private sealed class RecordingQuickUndoCommandService
        : IQuickUndoCommandService
    {
        public int RunCount { get; private set; }

        public Task<QuickUndoCommandResult> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunCount++;
            return Task.FromResult(
                new QuickUndoCommandResult(
                    QuickUndoCommandStatus.NoBackup));
        }
    }

    private sealed class RecordingToolExecutionService : IToolExecutionService
    {
        public int StopCount { get; private set; }

        public Task<ToolExecutionResult<T>> ExecuteAsync<T>(
            ToolExecutionRequest<T> request,
            IProgress<ToolExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "The host lifecycle test does not execute tool requests.");
        }

        public bool Cancel(string operationId)
        {
            return false;
        }

        public bool IsRunning(string operationId)
        {
            return false;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingQuickRunService : IQuickRunService
    {
        public int RunCount { get; private set; }

        public Task<QuickRunResult> RunAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunCount++;
            return Task.FromResult(
                new QuickRunResult(QuickRunStatus.Executed, "tool"));
        }
    }

    private sealed class RecordingGlobalHotkeyService : IGlobalHotkeyService
    {
        public Dictionary<string, HotkeySettings?> Hotkeys { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, Func<CancellationToken, Task>> Callbacks { get; } =
            new(StringComparer.Ordinal);

        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public void SetBinding(
            string id,
            HotkeySettings? hotkey,
            Func<CancellationToken, Task> callback)
        {
            Hotkeys[id] = hotkey;
            Callbacks[id] = callback;
        }

        public void Start()
        {
            Started = true;
        }

        public void Stop()
        {
            Stopped = true;
        }
    }
}
