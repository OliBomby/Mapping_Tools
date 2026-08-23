namespace Mapping_Tools.Core.MathUtil;

/// <summary>
///     Supplies small collection and text helpers used by legacy geometry code.
/// </summary>
public static class MyExtensions
{
    /// <summary>
    ///     Counts tokens separated by spaces, periods, or question marks.
    /// </summary>
    /// <param name="str">The text to tokenize.</param>
    /// <returns>The number of non-empty tokens.</returns>
    public static int WordCount(this string str)
    {
        return str.Split(new[] { ' ', '.', '?' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    ///     Exposes a vector list's count under the name expected by ported path code.
    /// </summary>
    /// <param name="list">The vector list.</param>
    /// <returns><see cref="List{T}.Count" />.</returns>
    public static int Length(this List<Vector2> list)
    {
        return list.Count;
    }

    /// <summary>
    ///     Copies a vector list into an independently mutable list.
    /// </summary>
    /// <param name="list">The source vectors.</param>
    /// <returns>A shallow list copy; vectors themselves are values.</returns>
    public static List<Vector2> Copy(this List<Vector2> list)
    {
        var newList = new List<Vector2>();
        newList.AddRange(list);
        return newList;
    }

    /// <summary>
    ///     Invokes the vector rounding operation for every value in a sequence.
    /// </summary>
    /// <param name="list">The vectors to round.</param>
    public static void Round(this IEnumerable<Vector2> list)
    {
        foreach (var v in list) v.Round();
    }
}
