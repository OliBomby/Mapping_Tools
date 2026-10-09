using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Infrastructure.Logging;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopHostFactory
{
    internal static IHost Create(string[] args)
    {
        string? localUpdatePackagePath = DesktopStartupArguments.GetLocalUpdatePackagePath(args);
        var builder = Host.CreateApplicationBuilder(args);
        string logsPath = Path.Combine(new ApplicationDirectories().ApplicationData, "Logs");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new SessionFileLoggerProvider(logsPath));
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Services.AddMappingToolsDesktop(
            ToolAssemblyLoader.Load(),
            localUpdatePackagePath);
        builder.Services.AddMappingToolsHostedServices();
        return builder.Build();
    }

    internal static IServiceCollection AddMappingToolsHostedServices(
        this IServiceCollection services)
    {
        services.AddHostedService<ToolExecutionHostedService>();
        services.AddHostedService<PeriodicBackupHostedService>();
        services.AddHostedService<BetterSaveOverrideHostedService>();
        services.AddHostedService<NotificationPresenter>();
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<GlobalHotkeyHostedService>());
        return services;
    }
}
