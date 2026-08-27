using System.Reflection;
using System.Runtime.Loader;

namespace Mapping_Tools.Desktop.Composition;

internal static class ToolAssemblyLoader
{
    internal static IReadOnlyList<Assembly> Load()
    {
        var assemblies = new List<Assembly> { typeof(ToolAssemblyLoader).Assembly };
        string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginDirectory))
            return assemblies;

        foreach (string path in Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)));
            }
            catch (Exception exception) when (exception is IOException
                                               or BadImageFormatException
                                               or FileLoadException)
            {
                throw new InvalidOperationException(
                    $"Could not load Mapping Tools plugin '{path}'.",
                    exception);
            }
        }

        return assemblies;
    }
}
