using System.Diagnostics;
using System.Reflection;
using Mapping_Tools.Desktop.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal sealed class ShellPluginCatalog
{
    private ShellPluginCatalog(IReadOnlyList<IShellPlugin> plugins)
    {
        Plugins = plugins;
    }

    internal IReadOnlyList<IShellPlugin> Plugins { get; }

    internal static ShellPluginCatalog Discover(
        IEnumerable<Assembly> assemblies,
        Action<Assembly, Exception>? logFailure = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        List<IShellPlugin> plugins = [];
        foreach (var assembly in assemblies.Distinct())
        {
            try
            {
                // Materialize first so a broken assembly cannot leave partial contributions.
                var discovered = assembly.GetTypes()
                    .Where(type => !type.IsAbstract && !type.ContainsGenericParameters
                                   && type.IsDefined(typeof(ShellPluginAttribute), false)
                                   && typeof(IShellPlugin).IsAssignableFrom(type))
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .Select(type => type.GetConstructor(Type.EmptyTypes) is not null
                        ? (IShellPlugin)Activator.CreateInstance(type)!
                        : throw new InvalidOperationException(
                            $"Shell plugin '{type.FullName}' must have a parameterless constructor."))
                    .ToArray();
                plugins.AddRange(discovered);
            }
            catch (Exception exception) when (assembly != typeof(ShellPluginCatalog).Assembly)
            {
                if (logFailure is not null) logFailure(assembly, exception);
                else Trace.TraceError("Could not load shell plugins from assembly '{0}': {1}",
                    assembly.GetName().Name, exception);
            }
        }

        return new ShellPluginCatalog(plugins);
    }

    internal void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        foreach (var plugin in Plugins) plugin.RegisterServices(services);
    }
}
