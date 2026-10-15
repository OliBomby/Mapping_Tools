using System.Diagnostics;
using System.Reflection;
using Mapping_Tools.Desktop.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal sealed class ShellExtensionCatalog
{
    private ShellExtensionCatalog(IReadOnlyList<IShellExtension> extensions)
    {
        Extensions = extensions;
    }

    internal IReadOnlyList<IShellExtension> Extensions { get; }

    internal static ShellExtensionCatalog Discover(
        IEnumerable<Assembly> assemblies,
        Action<Assembly, Exception>? logFailure = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        List<IShellExtension> extensions = [];
        foreach (var assembly in assemblies.Distinct())
        {
            try
            {
                // Materialize first so a broken assembly cannot leave partial contributions.
                var discovered = assembly.GetTypes()
                    .Where(type => !type.IsAbstract && !type.ContainsGenericParameters
                                   && type.IsDefined(typeof(ShellExtensionAttribute), false)
                                   && typeof(IShellExtension).IsAssignableFrom(type))
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .Select(type => type.GetConstructor(Type.EmptyTypes) is not null
                        ? (IShellExtension)Activator.CreateInstance(type)!
                        : throw new InvalidOperationException(
                            $"Shell extension '{type.FullName}' must have a parameterless constructor."))
                    .ToArray();
                extensions.AddRange(discovered);
            }
            catch (Exception exception) when (assembly != typeof(ShellExtensionCatalog).Assembly)
            {
                if (logFailure is not null) logFailure(assembly, exception);
                else Trace.TraceError("Could not load shell extensions from assembly '{0}': {1}",
                    assembly.GetName().Name, exception);
            }
        }

        return new ShellExtensionCatalog(extensions);
    }

    internal void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        foreach (var extension in Extensions) extension.RegisterServices(services);
    }
}
