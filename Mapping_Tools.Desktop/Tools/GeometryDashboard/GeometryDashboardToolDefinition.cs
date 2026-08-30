using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.GeometryDashboard;

/// <summary>Describes and composes the Geometry Dashboard plugin feature.</summary>
[MappingToolDefinition]
public sealed class GeometryDashboardToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public int Order => 280;

    /// <inheritdoc />
    public ToolDefinition Definition => GeometryDashboardToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(GeometryDashboardViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(GeometryDashboardView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<GeometryDashboardProject>();
        services.AddSingleton<GeometryDashboardServiceOptions>(provider =>
            provider.GetRequiredService<GeometryDashboardProject>());
        services.AddSingleton<IGeometryDashboardService, GeometryDashboardService>();
        services.AddSingleton<GeometryDashboardLifecycleCoordinator>();
        services.AddHostedService(provider =>
            provider.GetRequiredService<GeometryDashboardLifecycleCoordinator>());
        services.AddSingleton<IGeometryDashboardProcessDiscovery, WindowsOsuProcessDiscovery>();
        services.AddSingleton<IGeometryDashboardScreenService, WindowsGeometryDashboardScreenService>();
        services.AddSingleton<IGeometryDashboardWindowService, WindowsGeometryDashboardWindowService>();
        services.AddSingleton<IGeometryDashboardRuntime, WindowsGeometryDashboardRuntimeService>();
        services.AddSingleton<WindowsGeometryDashboardCoordinateContext>();
        services.AddSingleton<IGeometryDashboardInputService, WindowsGeometryDashboardInputService>();
        services.AddSingleton<IGeometryDashboardOverlayService, WindowsGeometryDashboardOverlayService>();
    }
}
