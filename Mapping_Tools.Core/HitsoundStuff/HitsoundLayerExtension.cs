using System.Globalization;
using System.Text;

namespace Mapping_Tools.Core.HitsoundStuff;

/// <summary>
///     Provides formatting helpers for multi-selection hitsound editors.
/// </summary>
public static class HitsoundLayerExtension
{
    /// <summary>
    ///     Formats a projected value only when every selected item has the same value.
    /// </summary>
    /// <typeparam name="TObj">The selected item type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="list">The selected items.</param>
    /// <param name="func">Projects the editable value.</param>
    /// <param name="culture">The culture used by <see cref="Convert.ToString(object, IFormatProvider)" />.</param>
    /// <returns>The common value, or an empty string for an empty or mixed selection.</returns>
    public static string AllToStringOrDefault<TObj, TResult>(this List<TObj> list, Func<TObj, TResult> func, CultureInfo culture = null)
    {
        if (list.Count == 0)
            return "";
        var first = func(list.First());
        foreach (var o in list)
            if (!func(o).Equals(first))
                return "";
        return Convert.ToString(first, culture);
    }

    /// <summary>
    ///     Converts a projected value only when every selected item has the same value.
    /// </summary>
    /// <typeparam name="TObj">The selected item type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="list">The selected items.</param>
    /// <param name="func">Projects the editable value.</param>
    /// <param name="stringConverter">Formats the common value.</param>
    /// <returns>The converted common value, or an empty string for an empty or mixed selection.</returns>
    public static string AllToStringOrDefault<TObj, TResult>(this List<TObj> list, Func<TObj, TResult> func, Func<TResult, string> stringConverter)
    {
        if (list.Count == 0)
            return "";
        var first = func(list.First());
        foreach (var o in list)
            if (!func(o).Equals(first))
                return "";
        return stringConverter(first);
    }

    /// <summary>
    ///     Joins floating-point values as an invariant-culture comma-separated list.
    /// </summary>
    /// <param name="list">Values to serialize.</param>
    /// <returns>The values in source order without a trailing comma.</returns>
    public static string DoubleListToStringConverter(List<double> list)
    {
        var accumulator = new StringBuilder(list.Count * 2); // Rough guess for capacity of StringBuilder
        foreach (double d in list) accumulator.Append(d.ToString(CultureInfo.InvariantCulture)).Append(",");
        if (accumulator.Length > 0)
            accumulator.Remove(accumulator.Length - 1, 1);
        return accumulator.ToString();
    }
}
