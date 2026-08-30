using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.SliderPicturator.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Views;
using Mapping_Tools.Infrastructure.Tools.SliderPicturator;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.SliderPicturator;

/// <summary>Describes and composes the Slider Picturator plugin feature.</summary>
[MappingToolDefinition]
public sealed class SliderPicturatorToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 220;

    /// <inheritdoc />
    public ToolDefinition Definition => SliderPicturatorToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(SliderPicturatorViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(SliderPicturatorView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISliderPicturatorService, SliderPicturatorService>();
        services.AddSingleton<IImageFileService, SkiaSharpImageFileService>();
    }
}
