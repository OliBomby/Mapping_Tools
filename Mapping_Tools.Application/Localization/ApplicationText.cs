using System.Globalization;

namespace Mapping_Tools.Application.Localization;

/// <summary>Formats translated composite strings without changing date or number conventions.</summary>
public static class ApplicationText
{
    /// <summary>Formats a translated message while preserving invariant date and number formatting.</summary>
    /// <param name="format">The translated composite format string, usually supplied by a generated resource getter.</param>
    /// <param name="arguments">Values substituted into the message's numbered placeholders.</param>
    /// <returns>The translated, invariantly formatted message.</returns>
    public static string Format(string format, params object?[] arguments)
    {
        return string.Format(CultureInfo.InvariantCulture, format, arguments);
    }
}
