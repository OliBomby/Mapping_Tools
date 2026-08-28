using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper;

/// <summary>Describes and composes the Hitsound Preview Helper plugin feature.</summary>
[MappingToolDefinition]
public sealed class HitsoundPreviewHelperToolRegistration : IMappingToolDefinition
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
    public int Order => 130;

    /// <inheritdoc />
    public ToolDefinition Definition => HitsoundPreviewHelperToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(HitsoundPreviewHelperViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(HitsoundPreviewHelperView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IHitsoundPreviewHelperService, HitsoundPreviewHelperService>();
    }
}
