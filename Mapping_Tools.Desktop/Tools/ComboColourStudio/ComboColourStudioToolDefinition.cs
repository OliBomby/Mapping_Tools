using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.Views;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.ComboColourStudio;

/// <summary>Describes and composes the Combo Colour Studio plugin feature.</summary>
[MappingToolDefinition]
public sealed class ComboColourStudioToolRegistration : IMappingToolDefinition
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
    public int Order => 250;

    /// <inheritdoc />
    public ToolDefinition Definition => ComboColourStudioToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(ComboColourStudioViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(ComboColourStudioView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IComboColourStudioService, ComboColourStudioService>();
    }
}
