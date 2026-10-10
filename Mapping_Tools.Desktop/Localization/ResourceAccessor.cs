using System.Reflection;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Resolves generated resource getters for XAML and settings whose names are discovered at runtime.</summary>
public static class ResourceAccessor
{
    /// <summary>Finds a generated public static string property without reading or caching its translated value.</summary>
    /// <param name="resourceType">The generated resource class containing the property.</param>
    /// <param name="propertyName">The resource property name from XAML or reflected feature metadata.</param>
    /// <returns>A getter that reads the current translation, or null if no public static string getter exists.</returns>
    public static Func<string>? FindGetter(Type resourceType, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        var property = resourceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        return property?.PropertyType == typeof(string) && property.GetMethod is { IsStatic: true } getter
            ? getter.CreateDelegate<Func<string>>()
            : null;
    }
}
