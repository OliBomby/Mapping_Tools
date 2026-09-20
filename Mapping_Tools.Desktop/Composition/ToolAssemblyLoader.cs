using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Desktop.Composition;

internal static class ToolAssemblyLoader
{
    internal static IReadOnlyList<Assembly> Load()
    {
        var directories = new ApplicationDirectories();
        return Load(Path.Combine(directories.ApplicationData, "Plugins"));
    }

    internal static IReadOnlyList<Assembly> Load(string pluginDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        var assemblies = new List<Assembly> { typeof(ToolAssemblyLoader).Assembly };
        string fullPluginDirectory = Path.GetFullPath(pluginDirectory);
        Directory.CreateDirectory(fullPluginDirectory);

        foreach (string path in Directory.EnumerateFiles(fullPluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            try
            {
                assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)));
            }
            catch (Exception exception)
            {
                Trace.TraceError(
                    "Could not load Mapping Tools plugin '{0}': {1}",
                    path,
                    exception);
            }

        return assemblies;
    }
}
