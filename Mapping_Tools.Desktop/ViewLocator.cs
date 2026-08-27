using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapping_Tools.Desktop;

/// <summary>
///     Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    ///     Resolves a view by replacing the runtime view-model type's
    ///     <c>ViewModel</c> suffix with <c>View</c> and searching loaded
    ///     assemblies, including external tool assemblies.
    /// </summary>
    /// <param name="param">The view model to present.</param>
    /// <returns>
    ///     The constructed view, a diagnostic text block when no matching type exists,
    ///     or <see langword="null" /> for null data.
    /// </returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name)
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .Select(assembly => GetType(assembly, name))
                       .FirstOrDefault(candidate => candidate is not null);

        if (type != null) return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = "Not Found: " + name };

        static Type? GetType(Assembly assembly, string fullName)
        {
            try
            {
                return assembly.GetType(fullName, throwOnError: false);
            }
            catch (ReflectionTypeLoadException)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Determines whether this template handles the supplied presentation object.
    /// </summary>
    /// <param name="data">The candidate data object.</param>
    /// <returns><see langword="true" /> for Mapping Tools view models.</returns>
    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}
