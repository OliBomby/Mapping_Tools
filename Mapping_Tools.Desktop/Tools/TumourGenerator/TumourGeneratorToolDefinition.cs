using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Views;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.TumourGenerator;

/// <summary>Describes and composes the Tumour Generator plugin feature.</summary>
[MappingToolDefinition]
public sealed class TumourGeneratorToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public int Order => 240;

    /// <inheritdoc />
    public ToolDefinition Definition => TumourGeneratorToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(TumourGeneratorViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(TumourGeneratorView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITumourGeneratorService, TumourGeneratorService>();
    }
}
