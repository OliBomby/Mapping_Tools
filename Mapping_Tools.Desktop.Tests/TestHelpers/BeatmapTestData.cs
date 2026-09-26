using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;

namespace Mapping_Tools.Desktop.Tests.TestHelpers;

internal static class BeatmapTestData
{
    private static readonly IBeatmapDecoder beatmapDecoder = new BeatmapDecoder();
    private static readonly IBeatmapEncoder beatmapEncoder = new BeatmapEncoder();

    internal static HitObject DecodeHitObject(string line, int formatVersion = 14)
    {
        string text = $"osu file format v{formatVersion}\r\n[Difficulty]\r\nSliderMultiplier: 1.4\r\n[HitObjects]\r\n{line}";
        return beatmapDecoder.Decode(text).HitObjects.Single();
    }

    internal static string EncodeHitObject(HitObject hitObject, int formatVersion = 128)
    {
        Beatmap beatmap = new() { Version = formatVersion };
        beatmap.HitObjects.Add(hitObject);
        string text = beatmapEncoder.Encode(beatmap);
        string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        int hitObjectsIndex = Array.IndexOf(lines, "[HitObjects]");
        if (hitObjectsIndex < 0 || hitObjectsIndex + 1 >= lines.Length)
            throw new InvalidOperationException("The encoded document did not contain the [HitObjects] section.");

        return lines.Skip(hitObjectsIndex + 1).First(line => line.Length > 0);
    }
}
