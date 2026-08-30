using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Projects.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Plugin;

/// <summary>
///     Supplies the presentation metadata and service registrations for one
///     tool discovered from a built-in or external assembly.
/// </summary>
public interface IMappingToolDefinition
{
    /// <summary>Gets the stable metadata used by shell and command catalogs.</summary>
    ToolDefinition Definition { get; }

    /// <summary>
    ///     Gets the schema used for the tool's persisted configuration. A tool
    ///     can replace the conventional schema to supply its own migrations.
    /// </summary>
    ToolConfigSchema ConfigSchema => ToolConfigSchema.ForTool(Definition.Id);

    /// <summary>Gets the shell's horizontal scrolling policy.</summary>
    ToolScrollBarVisibility HorizontalScrollBarVisibility { get; }

    /// <summary>Gets the shell's vertical scrolling policy.</summary>
    ToolScrollBarVisibility VerticalScrollBarVisibility { get; }

    /// <summary>Gets the stable navigation order used when definitions are discovered.</summary>
    int Order { get; }

    /// <summary>Gets the view-model type created for the tool's shell feature.</summary>
    Type ViewModelType { get; }

    /// <summary>Gets the frontend view type that presents <see cref="ViewModelType" />.</summary>
    Type ViewType { get; }

    /// <summary>
    ///     Adds the application, infrastructure, and other tool-owned services
    ///     required before the host validates the dependency graph.
    /// </summary>
    /// <param name="services">The host service collection receiving the registrations.</param>
    void RegisterServices(IServiceCollection services);
}
