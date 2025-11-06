using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper.ComboColours;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.IO.Decoding.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.Exceptions;
using static Mapping_Tools.Core.BeatmapHelper.IO.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding;

public class OsuBeatmapDecoder : IDecoder<Beatmap> {
    private readonly IDecoder<Storyboard> storyboardDecoder;
    private readonly IDecoder<HitObject> hitObjectDecoder;
    private readonly IDecoder<TimingPoint> timingPointDecoder;

    public OsuBeatmapDecoder() : this(new OsuStoryboardDecoder(), new HitObjectDecoder(), new TimingPointDecoder()) { }

    public OsuBeatmapDecoder(IDecoder<Storyboard> storyboardDecoder, IDecoder<HitObject> hitObjectDecoder, IDecoder<TimingPoint> timingPointDecoder) {
        this.storyboardDecoder = storyboardDecoder;
        this.hitObjectDecoder = hitObjectDecoder;
        this.timingPointDecoder = timingPointDecoder;
    }

    public void Decode(Beatmap beatmap, string code) {
        var lines = code.Split('\n').Select(l => l.Trim('\r')).ToList();

        // Get the beatmap version
        string headerInfo = lines[0];
        if (headerInfo.IndexOf("osu file format", StringComparison.Ordinal) == 0) {
            beatmap.BeatmapVersion = ParseInt(headerInfo.Remove(0, headerInfo.LastIndexOf('v') + 1));
        }

        // Load up all the shit
        IEnumerable<string> generalLines = GetCategoryLines(lines, "[General]");
        IEnumerable<string> editorLines = GetCategoryLines(lines, "[Editor]");
        IEnumerable<string> metadataLines = GetCategoryLines(lines, "[Metadata]");
        IEnumerable<string> difficultyLines = GetCategoryLines(lines, "[Difficulty]");
        IEnumerable<string> timingLines = GetCategoryLines(lines, "[TimingPoints]");
        IEnumerable<string> colourLines = GetCategoryLines(lines, "[Colours]");
        IEnumerable<string> hitobjectLines = GetCategoryLines(lines, "[HitObjects]");

        DecodeSection(beatmap, generalLines, DecodeGeneral);
        DecodeSection(beatmap, editorLines, DecodeEditor);
        DecodeSection(beatmap, metadataLines, DecodeMetadata);
        DecodeSection(beatmap, difficultyLines, DecodeDifficulty);

        foreach (string line in colourLines) {
            if (Regex.IsMatch(line.Substring(0, 6), @"^Combo[1-8]$")) {
                beatmap.ComboColoursList.Add(new ComboColour(line));
            } else {
                beatmap.SpecialColours[SplitKeyValue(line).Item1] = new ComboColour(line);
            }
        }

        foreach (string line in hitobjectLines) {
            beatmap.HitObjects.Add(hitObjectDecoder.Decode(line));
        }

        // Give the lines to the storyboard
        beatmap.Storyboard = storyboardDecoder.Decode(code);

        // Set the timing object
        beatmap.BeatmapTiming = new Timing(beatmap.Difficulty.SliderMultiplier);

        // Pass the default fallback values from the headers to the timing point decoder
        if (timingPointDecoder is IConfigurableTimingPointDecoder configurable) {
            configurable.DefaultSampleSet = beatmap.General.SampleSet;
            configurable.DefaultVolume = beatmap.General.SampleVolume;
        }

        foreach (var timingLine in timingLines) {
            beatmap.BeatmapTiming.Add(timingPointDecoder.Decode(timingLine));
        }

        beatmap.SortHitObjects();
        beatmap.CalculateHitObjectComboStuff();
        beatmap.GiveObjectsTimingContext();
    }

    public Beatmap Decode(string code) {
        var beatmap = new Beatmap();
        Decode(beatmap, code);

        return beatmap;
    }

    #region BigPropertyStuff

    private delegate void SectionDecoderDelegate(Beatmap b, string left, string right);

    private static void DecodeSection(Beatmap b, IEnumerable<string> generalLines, SectionDecoderDelegate sectionDecoder) {
        foreach (var line in generalLines) {
            try {
                var result = SplitKeyValue(line);
                if (result is null)
                    continue;
                sectionDecoder.Invoke(b, result.Item1, result.Item2);
            }
            catch (Exception e) {
                throw new BeatmapParsingException(line, e);
            }
        }
    }

    private static void DecodeGeneral(Beatmap b, string left, string right) {
        switch (left) {
            case "SampleSet":
                b.General.SampleSet = (SampleSet)Enum.Parse(typeof(SampleSet), right);
                break;
            case "CustomSamples":
                break;
            case "OverlayPosition":
                b.General.OverlayPosition = (OverlayPosition)Enum.Parse(typeof(OverlayPosition), right, true);
                break;
            case "Countdown":
                b.General.Countdown = (Countdown)ParseInt(right);
                break;
            case "AudioFilename":
                if (right.Length > 0)
                    b.General.AudioFilename = right;
                break;
            case "AudioHash":
                b.General.AudioHash = right;
                break;
            case "AudioLeadIn":
                b.General.AudioLeadIn = ParseInt(right);
                break;
            case "PreviewTime":
                b.General.PreviewTime = ParseInt(right);
                break;
            case "SampleVolume":
                b.General.SampleVolume = ParseInt(right);
                break;
            case "StackLeniency":
                b.General.StackLeniency = ParseFloat(right);
                break;
            case "Mode":
                b.General.Mode = (GameMode)ParseInt(right);
                break;
            case "LetterboxInBreaks":
                b.General.LetterboxInBreaks = right[0] == '1';
                break;
            case "WidescreenStoryboard":
                b.General.WidescreenStoryboard = right[0] == '1';
                break;
            case "SkinPreference":
                b.General.SkinPreference = right;
                break;
            case "AlwaysShowPlayfield":
                b.General.AlwaysShowPlayfield = right[0] == '1';
                break;
            case "EpilepsyWarning":
                b.General.EpilepsyWarning = right[0] == '1';
                break;
            case "CountdownOffset":
                b.General.CountdownOffset = ParseInt(right);
                break;
            case "SpecialStyle":
                b.General.SpecialStyle = right[0] == '1';
                break;
            case "TimelineZoom":
                b.Editor.TimelineZoom = ParseFloat(right);
                break;
            case "SamplesMatchPlaybackRate":
                b.General.SamplesMatchPlaybackRate = right[0] == '1';
                break;
            case @"EditorBookmarks":
                string[] strlist = right.Split(',');
                foreach (string s in strlist)
                    if (s.Length > 0) {
                        var bm = ParseDouble(s);
                        b.Editor.Bookmarks.Add(bm);
                    }
                break;
            case @"EditorDistanceSpacing":
                b.Editor.DistanceSpacing = ParseDouble(right);
                break;
            case @"StoryFireInFront":
                b.General.StoryFireInFront = right[0] == '1';
                break;
            case @"UseSkinSprites":
                b.General.UseSkinSprites = right[0] == '1';
                break;
        }
    }

    private static void DecodeEditor(Beatmap b, string left, string right) {
        switch (left) {
            case @"Bookmarks":
                string[] strlist = right.Split(',');
                foreach (string s in strlist)
                    if (s.Length > 0) {
                        var bm = ParseDouble(s);
                        b.Editor.Bookmarks.Add(bm);
                    }
                break;
            case @"DistanceSpacing":
                b.Editor.DistanceSpacing = ParseDouble(right);
                break;
            case @"BeatDivisor":
                b.Editor.BeatDivisor = ParseInt(right);
                break;
            case @"GridSize":
                b.Editor.GridSize = ParseInt(right);
                break;
            case @"TimelineZoom":
                b.Editor.TimelineZoom = ParseFloat(right);
                break;
        }
    }

    private static void DecodeMetadata(Beatmap b, string left, string right) {
        switch (left) {
            case "Artist":
                b.Metadata.Artist = right;
                b.Metadata.ArtistUnicode = right;
                break;
            case "ArtistUnicode":
                b.Metadata.ArtistUnicode = right;
                break;
            case "Title":
                b.Metadata.Title = right;
                b.Metadata.TitleUnicode = right;
                break;
            case "TitleUnicode":
                b.Metadata.TitleUnicode = right;
                break;
            case "Creator":
                b.Metadata.Creator = right;
                break;
            case "Version":
                b.Metadata.Version = right;
                break;
            case "Tags":
                b.Metadata.Tags = right;
                break;
            case "Source":
                b.Metadata.Source = right;
                break;
            case "BeatmapID":
                b.Metadata.BeatmapId = ParseInt(right);
                break;
            case "BeatmapSetID":
                b.Metadata.BeatmapSetId = ParseInt(right);
                break;
        }
    }

    private static void DecodeDifficulty(Beatmap b, string left, string right) {
        switch (left) {
            case "ApproachRate":
                b.Difficulty.ApproachRate = ParseFloat(right);
                break;
            case "HPDrainRate":
                b.Difficulty.HpDrainRate = ParseFloat(right);
                break;
            case "CircleSize": 
                b.Difficulty.CircleSize = ParseFloat(right);
                break;
            case "OverallDifficulty":
                b.Difficulty.OverallDifficulty = ParseFloat(right);
                break;
            case "SliderMultiplier":
                b.Difficulty.SliderMultiplier = ParseDouble(right);
                break;
            case "SliderTickRate":
                b.Difficulty.SliderTickRate = ParseDouble(right);
                break;
        }
    }

    #endregion
}