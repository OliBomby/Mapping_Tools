using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.PropertyTransformer.ViewModels;
using Mapping_Tools.Desktop.Tools.PropertyTransformer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.PropertyTransformer;

/// <summary>Describes and composes the Property Transformer plugin feature.</summary>
[MappingToolDefinition]
public sealed class PropertyTransformerToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 170;

    /// <inheritdoc />
    public ToolDefinition Definition => PropertyTransformerToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(PropertyTransformerViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(PropertyTransformerView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPropertyTransformerService, PropertyTransformerService>();
    }
}
