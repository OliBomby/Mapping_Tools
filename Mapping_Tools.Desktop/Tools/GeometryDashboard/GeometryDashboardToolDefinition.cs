using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Platform;
using Microsoft.Extensions.DependencyInjection;

using ApplicationDefinition = Mapping_Tools.Application.Tools.GeometryDashboard.GeometryDashboardToolDefinition;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard;

/// <summary>Describes and composes the Geometry Dashboard plugin feature.</summary>
[MappingToolDefinition]
public sealed class GeometryDashboardToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public string Category => "Tools";

    /// <inheritdoc />
    public bool StartsSection => false;

    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public int Order => 280;

    /// <inheritdoc />
    public ToolDefinition Definition => ApplicationDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(GeometryDashboardViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(GeometryDashboardView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IGeometryDashboardProcessDiscovery, WindowsOsuProcessDiscovery>();
        services.AddSingleton<IGeometryDashboardInputService, WindowsGeometryDashboardInputService>();
        services.AddSingleton<IGeometryDashboardScreenService, WindowsGeometryDashboardScreenService>();
        services.AddSingleton<IGeometryDashboardWindowService, WindowsGeometryDashboardWindowService>();
        services.AddSingleton<IGeometryDashboardRuntime, GeometryDashboardRuntimeService>();
        services.AddSingleton<IGeometryDashboardEditorReader>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<IGeometryDashboardOverlayHostFactory>(provider =>
            new WindowsGeometryDashboardOverlayHostFactory(
                provider.GetRequiredService<IGeometryDashboardWindowService>()));
    }
}
