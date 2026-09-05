using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.Views;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.SliderCompletionator;

/// <summary>Describes and composes the Slider Completionator plugin feature.</summary>
[MappingToolDefinition]
public sealed class SliderCompletionatorToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 200;

    /// <inheritdoc />
    public ToolDefinition Definition => SliderCompletionatorToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(SliderCompletionatorViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(SliderCompletionatorView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISliderCompletionatorService, SliderCompletionatorService>();
    }
}
