using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Desktop.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Registers the sample tool with the Mapping Tools desktop shell.
/// </summary>
[MappingToolDefinition]
public sealed class SampleToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public string Category => "Tools";

    /// <inheritdoc />
    public bool StartsSection => true;

    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 10_000;

    /// <inheritdoc />
    public ToolDefinition Definition => SampleToolDefinition.Definition;

    /// <inheritdoc />
    public ToolConfigSchema ConfigSchema => SampleToolConfigSchema.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(SampleToolViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(SampleToolView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<SampleToolService>();
    }
}
