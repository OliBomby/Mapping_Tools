using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.HitsoundStuff;

/// <summary>
/// Parses the standard osu! sample filename convention without accessing the filesystem.
/// </summary>
public static class HitsoundFilename {
    private static readonly Regex StandardName = new(
        "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads the sample-family prefix from a standard hitsound filename.
    /// </summary>
    /// <param name="filename">A filename beginning with <c>auto-</c>, <c>normal-</c>, <c>soft-</c>, or <c>drum-</c>.</param>
    /// <returns>The decoded set; unknown prefixes fall back to soft for legacy compatibility.</returns>
    public static SampleSet GetSampleSet(string filename) {
        string[] split = filename.Split('-');
        if (split.Length < 1) {
            return SampleSet.Soft;
        }

        return split[0] switch {
            "auto" => SampleSet.None,
            "normal" => SampleSet.Normal,
            "soft" => SampleSet.Soft,
            "drum" => SampleSet.Drum,
            _ => SampleSet.Soft
        };
    }

    /// <summary>
    /// Detects the whistle, finish, or clap token in a standard filename.
    /// </summary>
    /// <param name="filename">A standard hyphen-separated sample filename.</param>
    /// <returns>The detected addition, or normal when no recognized addition appears.</returns>
    public static Hitsound GetHitsound(string filename) {
        string[] split = filename.Split('-');
        if (split.Length < 2) {
            return Hitsound.Normal;
        }

        string hitsound = split[1];
        if (hitsound.Contains("hitwhistle")) {
            return Hitsound.Whistle;
        }

        if (hitsound.Contains("hitfinish")) {
            return Hitsound.Finish;
        }

        if (hitsound.Contains("hitclap")) {
            return Hitsound.Clap;
        }

        return Hitsound.Normal;
    }

    /// <summary>
    /// Parses the numeric suffix after the recognized standard sample name.
    /// </summary>
    /// <param name="filename">A standard hitsound filename, with or without extension.</param>
    /// <returns>The parsed suffix, or zero when absent or malformed.</returns>
    public static int GetIndex(string filename) {
        Match match = StandardName.Match(filename);
        string remainder = filename.Substring(match.Index + match.Length);
        int index = 0;

        if (!string.IsNullOrEmpty(remainder)) {
            FileFormatHelper.TryParseInt(remainder, out index);
        }

        return index;
    }
}
