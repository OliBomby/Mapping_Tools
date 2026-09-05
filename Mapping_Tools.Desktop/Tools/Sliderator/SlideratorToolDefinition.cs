using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;
using Mapping_Tools.Desktop.Tools.Sliderator.Views;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.Sliderator;

/// <summary>Describes and composes the Sliderator plugin feature.</summary>
[MappingToolDefinition]
public sealed class SlideratorToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public int Order => 230;

    /// <inheritdoc />
    public ToolDefinition Definition => SlideratorToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(SlideratorViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(SlideratorView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISlideratorService, SlideratorService>();
    }
}
