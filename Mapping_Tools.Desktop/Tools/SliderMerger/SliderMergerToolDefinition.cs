using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.SliderMerger.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderMerger.Views;
using Microsoft.Extensions.DependencyInjection;

using ApplicationDefinition = Mapping_Tools.Application.Tools.SliderMerger.SliderMergerToolDefinition;

namespace Mapping_Tools.Desktop.Tools.SliderMerger;

/// <summary>Describes and composes the Slider Merger plugin feature.</summary>
[MappingToolDefinition]
public sealed class SliderMergerToolRegistration : IMappingToolDefinition
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
    public int Order => 210;

    /// <inheritdoc />
    public ToolDefinition Definition => ApplicationDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(SliderMergerViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(SliderMergerView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISliderMergerService, SliderMergerService>();
    }
}
