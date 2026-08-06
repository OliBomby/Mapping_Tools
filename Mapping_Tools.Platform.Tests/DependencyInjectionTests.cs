using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

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
            typeof(AutoFailDetectorViewModel),
            typeof(MapCleanerViewModel),
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
            typeof(ApplicationSettings),
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
            typeof(IBeatmapFileSystem),
            typeof(ICurrentBeatmapLocator),
            typeof(IBetterSaveService),
            typeof(IRhythmGuideService),
            typeof(IAutoFailService),
            typeof(IMapCleanerService),
            typeof(IMapCleanerSampleService),
            typeof(IBetterSaveOverrideService),
            typeof(IBeatmapWorkspace),
            typeof(IProjectSerializer),
            typeof(IProjectStore),
            typeof(IProjectService)
        ];

        // Act
        services.AddMappingToolsDesktop();

        // Assert
        foreach (Type serviceType in expectedSingletons)
        {
            ServiceDescriptor? registration = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == serviceType);

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
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
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
        Action act = () =>
        {
            using ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
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
        ServiceDescriptor[] hosted = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .ToArray();
        // Assert
        hosted.Length.Should().Be(4);
        hosted.All(
            descriptor => descriptor.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
    }

    [TestMethod]
    public async Task StopAsync_DuringHostShutdown_StopsToolExecution()
    {
        // Arrange
        RecordingToolExecutionService execution = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IToolExecutionService>(execution);
        builder.Services.AddHostedService<ToolExecutionHostedService>();
        using IHost host = builder.Build();

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
        ApplicationSettings settings = new()
        {
            SongsPath = @"C:\osu!\Songs",
            OverrideOsuSave = true
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
        ApplicationSettings settings = new()
        {
            QuickRunHotkey = new HotkeySettings(56, 2),
            QuickUndoHotkey = new HotkeySettings(69, 6),
            BetterSaveHotkey = new HotkeySettings(31, 2)
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

        public bool Cancel(string operationId) => false;

        public bool IsRunning(string operationId) => false;

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

        public void Start() => Started = true;

        public void Stop() => Stopped = true;
    }
}
