using System.Globalization;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.BeatmapHelper.Events;

namespace Mapping_Tools.Core.Classes.BeatmapHelper;

/// <summary>
///     Helper class for File Formats
/// </summary>
#nullable disable
public static class FileFormatHelper
{
    private static readonly string[] osuDictionaryKeyOrder =
    {
        "AudioFilename",
        "AudioLeadIn",
        "AudioHash",
        "PreviewTime",
        "Countdown",
        "SampleSet",
        "StackLeniency",
        "Mode",
        "LetterboxInBreaks",
        "StoryFireInFront",
        "UseSkinSprites",
        "AlwaysShowPlayfield",
        "OverlayPosition",
        "SkinPreference",
        "EpilepsyWarning",
        "CountdownOffset",
        "SpecialStyle",
        "WidescreenStoryboard",
        "SamplesMatchPlaybackRate",
        "Bookmarks",
        "DistanceSpacing",
        "BeatDivisor",
        "GridSize",
        "TimelineZoom",
        "Title",
        "TitleUnicode",
        "Artist",
        "ArtistUnicode",
        "Creator",
        "Version",
        "Source",
        "Tags",
        "BeatmapID",
        "BeatmapSetID",
        "HPDrainRate",
        "CircleSize",
        "OverallDifficulty",
        "ApproachRate",
        "SliderMultiplier",
        "SliderTickRate",
    };

    private static readonly HashSet<string> osuDictionaryKeyOrderSet = new(osuDictionaryKeyOrder, StringComparer.Ordinal);

    /// <summary>
    ///     Converts the object to an Invariant string.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToInvariant(this object obj)
    {
        return Convert.ToString(obj, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts the object to the rounded Invariant string.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToRoundInvariant(this double obj)
    {
        return Math.Round(obj).ToInvariant();
    }

    /// <summary>
    ///     Converts the string into
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToIntInvariant(this SampleSet obj)
    {
        return ((int)obj).ToInvariant();
    }

    /// <summary>
    ///     Formats a storyboard layer as its numeric osu! file-format value.
    /// </summary>
    /// <param name="obj">The layer to encode.</param>
    /// <returns>The underlying enum integer using invariant culture.</returns>
    public static string ToIntInvariant(this StoryboardLayer obj)
    {
        return ((int)obj).ToInvariant();
    }

    /// <summary>
    ///     Attempts to parse an osu! floating-point field using invariant culture and exponent support.
    /// </summary>
    /// <param name="str">The field text.</param>
    /// <param name="result">The parsed number when successful.</param>
    /// <returns><see langword="true" /> when the entire field is a valid floating-point value.</returns>
    public static bool TryParseDouble(string str, out double result)
    {
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    ///     Attempts to parse a signed osu! integer field using invariant culture.
    /// </summary>
    /// <param name="str">The field text.</param>
    /// <param name="result">The parsed integer when successful.</param>
    /// <returns><see langword="true" /> when the entire field is a valid integer.</returns>
    public static bool TryParseInt(string str, out int result)
    {
        return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    ///     Serializes a key/value section in canonical osu! key order, followed by unknown keys in enumeration order.
    /// </summary>
    /// <param name="dict">The section values keyed by their file-format name.</param>
    /// <param name="lines">The output list to append to.</param>
    /// <param name="spaceBeforeValue">Whether to emit a space after the colon.</param>
    public static void AddDictionaryToLines(Dictionary<string, StringValue> dict, List<string> lines, bool spaceBeforeValue = false)
    {
        foreach (string key in osuDictionaryKeyOrder)
            if (dict.TryGetValue(key, out var value))
                lines.Add(key + GetSpacer(spaceBeforeValue) + value.Value);

        lines.AddRange(dict
            .Where(kvp => !osuDictionaryKeyOrderSet.Contains(kvp.Key))
            .Select(kvp => kvp.Key + GetSpacer(spaceBeforeValue) + kvp.Value.Value));
    }

    private static string GetSpacer(bool spaceBeforeValue)
    {
        return spaceBeforeValue ? ": " : ":";
    }

    /// <summary>
    ///     Replaces or adds dictionary entries parsed from colon-separated section lines.
    /// </summary>
    /// <param name="dict">The destination section dictionary.</param>
    /// <param name="lines">Raw key/value lines.</param>
    public static void FillDictionary(Dictionary<string, StringValue> dict, IEnumerable<string> lines)
    {
        foreach ((string key, string value) in lines.Select(SplitKeyValue)) dict[key] = new StringValue(value);
    }

    /// <summary>
    ///     Splits at the first colon so additional colons remain part of the value.
    /// </summary>
    /// <param name="line">A raw section line.</param>
    /// <returns>A trimmed key and value; lines without a colon have an empty value.</returns>
    public static (string, string) SplitKeyValue(string line)
    {
        int index = line.IndexOf(':');
        return index == -1 ? (line.Trim(), string.Empty) : (line[..index].Trim(), line[(index + 1)..].Trim());
    }

    /// <summary>
    ///     Streams non-empty lines after a section header until another recognized header begins.
    /// </summary>
    /// <param name="lines">The complete beatmap text.</param>
    /// <param name="category">The exact section header to locate, including brackets.</param>
    /// <param name="categoryIdentifiers">Prefixes that identify the next section; defaults to <c>[</c>.</param>
    /// <returns>The section's non-empty content lines in source order.</returns>
    public static IEnumerable<string> GetCategoryLines(IEnumerable<string> lines, string category, string[] categoryIdentifiers = null)
    {
        if (categoryIdentifiers == null)
            categoryIdentifiers = new[] { "[" };

        bool atCategory = false;

        foreach (string line in lines)
            if (atCategory && line != "")
            {
                if (categoryIdentifiers.Any(o => line.StartsWith(o))) // Reached another category
                    yield break;
                yield return line;
            }
            else
            {
                if (line == category) atCategory = true;
            }
    }

    /// <summary>
    ///     Filters source lines by any of the supplied ordinal prefixes.
    /// </summary>
    /// <param name="lines">The source lines.</param>
    /// <param name="prefixes">Accepted line prefixes.</param>
    /// <returns>Matching lines in source order.</returns>
    public static IEnumerable<string> GetLinesPrefix(IEnumerable<string> lines, string[] prefixes)
    {
        foreach (string line in lines)
            if (prefixes.Any(o => line.StartsWith(o)))
                yield return line;
    }

    /// <summary>
    ///     Checks whether an exact section-header line is present.
    /// </summary>
    /// <param name="lines">The complete beatmap text.</param>
    /// <param name="category">The exact header to find.</param>
    /// <returns><see langword="true" /> when a line equals the header.</returns>
    public static bool CategoryExists(IEnumerable<string> lines, string category)
    {
        return lines.Any(l => l == category);
    }
}
