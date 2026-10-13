using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Extends the desktop shell without requiring a mapping tool, view, or project.</summary>
public interface IShellPlugin
{
    /// <summary>Registers plugin-owned services and singleton <see cref="ShellMenuContribution" /> factories.</summary>
    /// <param name="services">The host services, resolved after all plugins have registered.</param>
    void RegisterServices(IServiceCollection services);
}
