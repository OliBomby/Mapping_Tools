using Mapping_Tools.Desktop.Composition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal static class DesktopHostFactory
{
    internal static IHost Create(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddMappingToolsDesktop();
        builder.Services.AddMappingToolsHostedServices();
        return builder.Build();
    }

    internal static IServiceCollection AddMappingToolsHostedServices(
        this IServiceCollection services)
    {
        services.AddHostedService<ToolExecutionHostedService>();
        services.AddHostedService<PeriodicBackupHostedService>();
        return services;
    }
}
