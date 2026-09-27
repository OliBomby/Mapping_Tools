using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Decodes complete osu! beatmap documents.</summary>
public sealed class BeatmapDecoder : IBeatmapDecoder
{
    private readonly IStoryboardDecoder storyboardDecoder;

    /// <summary>Creates a decoder with the default storyboard decoder.</summary>
    public BeatmapDecoder() : this(new StoryboardDecoder()) { }

    /// <summary>Creates a decoder that uses the supplied storyboard decoder.</summary>
    /// <param name="storyboardDecoder">The decoder for the embedded events section.</param>
    public BeatmapDecoder(IStoryboardDecoder storyboardDecoder)
    {
        this.storyboardDecoder = storyboardDecoder ?? throw new ArgumentNullException(nameof(storyboardDecoder));
    }

    /// <inheritdoc />
    public Beatmap Decode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        Beatmap beatmap = new();

        // The source header determines version-specific slider syntax and numeric precision.
        beatmap.Version = TryParseInt(lines[0][17..].Trim(), out int version) ? version : 14;

        // Normalize beatmaps older than the oldest format this parser supports.
        if (beatmap.Version < 14)
            beatmap.Version = 14;

        // Collect each section before building the objects that depend on it.
        var generalLines = GetCategoryLines(lines, "[General]");
        var editorLines = GetCategoryLines(lines, "[Editor]");
        var metadataLines = GetCategoryLines(lines, "[Metadata]");
        var difficultyLines = GetCategoryLines(lines, "[Difficulty]");
        var timingLines = GetCategoryLines(lines, "[TimingPoints]").ToList();
        var colourLines = GetCategoryLines(lines, "[Colours]");
        var hitObjectLines = GetCategoryLines(lines, "[HitObjects]");

        FillDictionary(beatmap.General, generalLines);
        FillDictionary(beatmap.Editor, editorLines);
        FillDictionary(beatmap.Metadata, metadataLines);
        FillDictionary(beatmap.Difficulty, difficultyLines);

        foreach (string line in colourLines)
        {
            if (line.Substring(0, 5) == "Combo")
            {
                beatmap.ComboColours.Add(new ComboColour(line));
            }
            else
            {
                beatmap.SpecialColours[SplitKeyValue(line).Item1] = new ComboColour(line);
            }
        }

        foreach (string line in hitObjectLines)
        {
            beatmap.HitObjects.Add(HitObjectTextCodec.Decode(line, beatmap.Version));
        }

        // Storyboard data shares the same source document but has its own codec.
        beatmap.StoryBoard = storyboardDecoder.Decode(text);
        var timingPoints = timingLines.Select(TimingPointTextCodec.Decode).ToList();
        beatmap.BeatmapTiming = new Timing(timingPoints, beatmap.Difficulty["SliderMultiplier"].DoubleValue);

        // Rebuild derived hit-object state after all source sections are available.
        beatmap.SortHitObjects();
        beatmap.CalculateHitObjectComboStuff();
        beatmap.CalculateSliderEndTimes();
        beatmap.GiveObjectsGreenlines();
        return beatmap;
    }
}
