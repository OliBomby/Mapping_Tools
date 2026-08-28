using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.HitsoundCopier.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundCopier.Views;
using Mapping_Tools.Infrastructure.Tools.HitsoundCopier;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.HitsoundCopier;

/// <summary>Describes and composes the Hitsound Copier plugin feature.</summary>
[MappingToolDefinition]
public sealed class HitsoundCopierToolRegistration : IMappingToolDefinition
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
    public int Order => 150;

    /// <inheritdoc />
    public ToolDefinition Definition => HitsoundCopierToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(HitsoundCopierViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(HitsoundCopierView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IHitsoundCopierService, HitsoundCopierService>();
        services.AddSingleton<IHitsoundSampleService, PhysicalHitsoundSampleService>();
    }
}
