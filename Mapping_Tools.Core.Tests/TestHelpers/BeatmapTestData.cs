using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.Serialization;

namespace Mapping_Tools.Core.Tests.TestHelpers;

internal static class BeatmapTestData
{
    private static readonly IBeatmapDecoder beatmapDecoder = new BeatmapDecoder();
    private static readonly IBeatmapEncoder beatmapEncoder = new BeatmapEncoder();
    private static readonly IStoryboardDecoder storyboardDecoder = new StoryboardDecoder();
    private static readonly IStoryboardEncoder storyboardEncoder = new StoryboardEncoder();

    internal static HitObject DecodeHitObject(string line, int formatVersion = 14)
    {
        string text = $"osu file format v{formatVersion}\r\n[Difficulty]\r\nSliderMultiplier: 1.4\r\n[HitObjects]\r\n{line}";
        return beatmapDecoder.Decode(text).HitObjects.Single();
    }

    internal static string EncodeHitObject(HitObject hitObject, int formatVersion = 128)
    {
        Beatmap beatmap = new() { Version = formatVersion };
        beatmap.HitObjects.Add(hitObject);
        return GetFirstSectionLine(beatmapEncoder.Encode(beatmap), "[HitObjects]");
    }

    internal static string EncodeTimingPoint(TimingPoint timingPoint, int formatVersion = 128)
    {
        Beatmap beatmap = new([], [timingPoint], timingPoint) { Version = formatVersion };
        return GetFirstSectionLine(beatmapEncoder.Encode(beatmap), "[TimingPoints]");
    }

    internal static TimingPoint DecodeTimingPoint(string line)
    {
        string text = $"osu file format v128\r\n[TimingPoints]\r\n{line}";
        return beatmapDecoder.Decode(text).BeatmapTiming.TimingPoints.Single();
    }

    internal static Event DecodeStoryboardEvent(string line)
    {
        string text = $"[Events]\r\n//Storyboard Layer 0 (Background)\r\n{line}";
        return storyboardDecoder.Decode(text).StoryboardLayerBackground.Single();
    }

    internal static Break DecodeBreak(string line)
    {
        string text = $"[Events]\r\n{line}";
        return storyboardDecoder.Decode(text).BreakPeriods.Single();
    }

    internal static string EncodeStoryboardEvent(Event storyboardEvent, int targetVersion = 128)
    {
        StoryBoard storyboard = new();
        storyboard.StoryboardLayerBackground.Add(storyboardEvent);
        string text = storyboardEncoder.Encode(storyboard, targetVersion);
        return GetFirstSectionLine(text, "//Storyboard Layer 0 (Background)");
    }

    private static string GetFirstSectionLine(string text, string sectionHeader)
    {
        string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        int sectionHeaderIndex = Array.IndexOf(lines, sectionHeader);
        if (sectionHeaderIndex < 0 || sectionHeaderIndex + 1 >= lines.Length)
            throw new InvalidOperationException($"The encoded document did not contain the '{sectionHeader}' section.");

        return lines.Skip(sectionHeaderIndex + 1).First(line => line.Length > 0);
    }
}
