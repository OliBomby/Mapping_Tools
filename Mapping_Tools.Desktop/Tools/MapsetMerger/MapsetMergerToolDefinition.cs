using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;
using Mapping_Tools.Desktop.Tools.MapsetMerger.Views;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.MapsetMerger;

/// <summary>Describes and composes the Mapset Merger plugin feature.</summary>
[MappingToolDefinition]
public sealed class MapsetMergerToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 260;

    /// <inheritdoc />
    public ToolDefinition Definition => MapsetMergerToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(MapsetMergerViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(MapsetMergerView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMapsetMergerService, MapsetMergerService>();
    }
}
