using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.MapCleaner.ViewModels;
using Mapping_Tools.Desktop.Tools.MapCleaner.Views;
using Mapping_Tools.Infrastructure.Tools.MapCleaner;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.MapCleaner;

/// <summary>Describes and composes the Map Cleaner plugin feature.</summary>
[MappingToolDefinition]
public sealed class MapCleanerToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public string Category => "Tools";

    /// <inheritdoc />
    public bool StartsSection => false;

    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 110;

    /// <inheritdoc />
    public ToolDefinition Definition => MapCleanerToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(MapCleanerViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(MapCleanerView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMapCleanerService, MapCleanerService>();
        services.AddSingleton<IMapCleanerSampleService, PhysicalMapCleanerSampleService>();
    }
}
