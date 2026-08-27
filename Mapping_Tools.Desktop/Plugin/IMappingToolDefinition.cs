using Mapping_Tools.Application.Tools;
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

    /// <summary>Gets the shell navigation category containing the tool.</summary>
    string Category { get; }

    /// <summary>Gets whether the shell draws a divider before this tool.</summary>
    bool StartsSection { get; }

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
