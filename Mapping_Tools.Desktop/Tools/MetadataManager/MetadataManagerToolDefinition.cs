using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.MetadataManager.ViewModels;
using Mapping_Tools.Desktop.Tools.MetadataManager.Views;
using Microsoft.Extensions.DependencyInjection;
using ApplicationDefinition = Mapping_Tools.Application.Tools.MetadataManager.MetadataManagerToolDefinition;

namespace Mapping_Tools.Desktop.Tools.MetadataManager;

/// <summary>Describes and composes the Metadata Manager plugin feature.</summary>
[MappingToolDefinition]
public sealed class MetadataManagerToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public string Category => "Tools";

    /// <inheritdoc />
    public bool StartsSection => false;

    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 160;

    /// <inheritdoc />
    public ToolDefinition Definition => ApplicationDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(MetadataManagerViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(MetadataManagerView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMetadataManagerService, MetadataManagerService>();
    }
}
