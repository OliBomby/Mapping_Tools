using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Projects;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Hosting;
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
    public void DesktopCompositionRootRegistersExpectedSingletons()
    {
        ServiceCollection services = new();

        services.AddMappingToolsDesktop();

        Type[] expectedSingletons =
        [
            typeof(MainWindow),
            typeof(MainViewModel),
            typeof(IFilePicker),
            typeof(IClipboardService),
            typeof(IPlatformLauncher),
            typeof(IFileRevealService),
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
            typeof(IBeatmapBackupStore),
            typeof(IBeatmapBackupService),
            typeof(ILiveBeatmapReader),
            typeof(IEditorReloadService),
            typeof(IBeatmapEditingGateway),
            typeof(IBeatmapFileSystem),
            typeof(ICurrentBeatmapLocator),
            typeof(IBeatmapWorkspace),
            typeof(IProjectSerializer),
            typeof(IProjectStore),
            typeof(IProjectService)
        ];

        foreach (Type serviceType in expectedSingletons)
        {
            ServiceDescriptor? registration = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == serviceType);

            Assert.IsNotNull(registration, $"{serviceType.Name} is not registered.");
            Assert.AreEqual(
                ServiceLifetime.Singleton,
                registration.Lifetime,
                $"{serviceType.Name} has the wrong lifetime.");
        }
    }

    [TestMethod]
    public void DesktopCompositionRootPassesContainerValidation()
    {
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        Assert.IsNotNull(provider);
    }

    [TestMethod]
    public void DesktopHostRegistersExecutionAndPeriodicBackupLifecycles()
    {
        ServiceCollection services = new();

        services.AddMappingToolsHostedServices();

        ServiceDescriptor[] hosted = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .ToArray();
        Assert.AreEqual(2, hosted.Length);
        Assert.IsTrue(hosted.All(
            descriptor => descriptor.Lifetime == ServiceLifetime.Singleton));
    }

    [TestMethod]
    public async Task GenericHostStopsToolExecutionDuringApplicationShutdown()
    {
        RecordingToolExecutionService execution = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IToolExecutionService>(execution);
        builder.Services.AddHostedService<ToolExecutionHostedService>();
        using IHost host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.AreEqual(1, execution.StopCount);
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
}
