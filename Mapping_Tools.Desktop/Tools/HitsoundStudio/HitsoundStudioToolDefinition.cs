using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio;

/// <summary>Describes and composes the Hitsound Studio plugin feature.</summary>
[MappingToolDefinition]
public sealed class HitsoundStudioToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 140;

    /// <inheritdoc />
    public ToolDefinition Definition => HitsoundStudioToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(HitsoundStudioViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(HitsoundStudioView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<HitsoundStudioEngine>();
        services.AddSingleton<IHitsoundStudioService, HitsoundStudioService>();
    }
}
