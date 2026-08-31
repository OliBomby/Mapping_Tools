using Mapping_Tools.Desktop.Services.Hosted;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopHostFactory
{
    internal static IHost Create(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddMappingToolsDesktop(ToolAssemblyLoader.Load());
        builder.Services.AddMappingToolsHostedServices();
        return builder.Build();
    }

    internal static IServiceCollection AddMappingToolsHostedServices(
        this IServiceCollection services)
    {
        services.AddHostedService<ToolExecutionHostedService>();
        services.AddHostedService<PeriodicBackupHostedService>();
        services.AddHostedService<BetterSaveOverrideHostedService>();
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<GlobalHotkeyHostedService>());
        return services;
    }
}
