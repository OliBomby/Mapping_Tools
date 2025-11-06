using System.Text;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Parsing.Encoding.HitObjects;
using Mapping_Tools.Domain.Beatmaps.TimingStuff;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.Encoding;

public class BeatmapEncoder : IEnumeratingEncoder<IBeatmap> {
    private readonly IEnumeratingEncoder<IStoryboard> storyboardEncoder;
    private readonly IEncoder<HitObject> hitObjectEncoder;
    private readonly IEncoder<TimingPoint> timingPointEncoder;

    public BeatmapEncoder() : this(new StoryboardEncoder(), new HitObjectEncoder(), new TimingPointEncoder()) { }

    public BeatmapEncoder(IEnumeratingEncoder<IStoryboard> storyboardEncoder,
        IEncoder<HitObject> hitObjectEncoder,
        IEncoder<TimingPoint> timingPointEncoder) {
        this.storyboardEncoder = storyboardEncoder;
        this.hitObjectEncoder = hitObjectEncoder;
        this.timingPointEncoder = timingPointEncoder;
    }

    public IEnumerable<string> EncodeEnumerable(IBeatmap beatmap) {
        // Getting all the stuff
        yield return @"osu file format v" + beatmap.BeatmapVersion.ToInvariant();
        yield return @"";

        yield return @"[General]";
        yield return @"AudioFilename: " + beatmap.General.AudioFilename;
        yield return @"AudioLeadIn: " + beatmap.General.AudioLeadIn.ToInvariant();
        //yield return "AudioHash: " + AudioEngine.AudioMd5;
        yield return @"PreviewTime: " + beatmap.General.PreviewTime.ToInvariant();
        yield return @"Countdown: " + ((int)beatmap.General.Countdown).ToInvariant();
        yield return @"SampleSet: " + beatmap.General.SampleSet;
        yield return @"StackLeniency: " + beatmap.General.StackLeniency.ToInvariant();
        yield return @"Mode: " + ((int)beatmap.General.Mode).ToInvariant();
        yield return @"LetterboxInBreaks: " + (beatmap.General.LetterboxInBreaks ? @"1" : @"0");
        if (!beatmap.General.StoryFireInFront)
            yield return @"StoryFireInFront: 0";
        if (beatmap.General.UseSkinSprites)
            yield return @"UseSkinSprites: 1";
        if (beatmap.General.AlwaysShowPlayfield)
            yield return @"AlwaysShowPlayfield: 1";
        if (beatmap.General.OverlayPosition != OverlayPosition.NoChange)
            yield return @"OverlayPosition: " + beatmap.General.OverlayPosition;
        if (!string.IsNullOrEmpty(beatmap.General.SkinPreference))
            yield return @"SkinPreference:" + beatmap.General.SkinPreference;
        if (beatmap.General.EpilepsyWarning)
            yield return @"EpilepsyWarning: 1";
        if (beatmap.General.CountdownOffset > 0)
            yield return @"CountdownOffset: " + beatmap.General.CountdownOffset.ToInvariant();
        if (beatmap.General.Mode == GameMode.Mania)
            yield return @"SpecialStyle: " + (beatmap.General.SpecialStyle ? @"1" : @"0");
        if (beatmap.BeatmapVersion > 10 || beatmap.General.WidescreenStoryboard)
            yield return @"WidescreenStoryboard: " + (beatmap.General.WidescreenStoryboard ? @"1" : @"0");
        if (beatmap.General.SamplesMatchPlaybackRate)
            yield return @"SamplesMatchPlaybackRate: 1";
        yield return @"";

        yield return @"[Editor]";

        if (beatmap.Editor.Bookmarks.Count > 0) {
            var builder = new StringBuilder();
            builder.AppendJoin(',', beatmap.Editor.Bookmarks.Select(b => InvariantHelper.ToRoundInvariant((double) b)));
            yield return @"Bookmarks: " + builder;
        }

        yield return @"DistanceSpacing: " + beatmap.Editor.DistanceSpacing.ToInvariant();
        yield return @"BeatDivisor: " + beatmap.Editor.BeatDivisor.ToInvariant();
        yield return @"GridSize: " + beatmap.Editor.GridSize.ToInvariant();
        if (beatmap.BeatmapVersion > 10 || beatmap.Editor.TimelineZoom != 1f)
            yield return @"TimelineZoom: " + beatmap.Editor.TimelineZoom.ToInvariant();
        yield return @"";

        yield return "[Metadata]";
        yield return @"Title:" + beatmap.Metadata.Title;
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.TitleUnicode != beatmap.Metadata.Title)
            yield return @"TitleUnicode:" + beatmap.Metadata.TitleUnicode;
        yield return @"Artist:" + beatmap.Metadata.Artist;
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.ArtistUnicode != beatmap.Metadata.Artist)
            yield return @"ArtistUnicode:" + beatmap.Metadata.ArtistUnicode;
        yield return @"Creator:" + beatmap.Metadata.Creator;
        yield return @"Version:" + beatmap.Metadata.Version;
        yield return @"Source:" + beatmap.Metadata.Source;
        yield return @"Tags:" + beatmap.Metadata.Tags;
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.BeatmapId != 0)
            yield return @"BeatmapID:" + beatmap.Metadata.BeatmapId.ToInvariant();
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.BeatmapSetId != -1)
            yield return @"BeatmapSetID:" + beatmap.Metadata.BeatmapSetId.ToInvariant();
        yield return @"";

        yield return "[Difficulty]";
        yield return (@"HPDrainRate:" + beatmap.Difficulty.HpDrainRate.ToInvariant());
        yield return (@"CircleSize:" + beatmap.Difficulty.CircleSize.ToInvariant());
        yield return (@"OverallDifficulty:" + beatmap.Difficulty.OverallDifficulty.ToInvariant());
        yield return (@"ApproachRate:" + beatmap.Difficulty.ApproachRate.ToInvariant());
        yield return (@"SliderMultiplier:" + beatmap.Difficulty.SliderMultiplier.ToInvariant());
        yield return (@"SliderTickRate:" + beatmap.Difficulty.SliderTickRate.ToInvariant());
        yield return @"";

        foreach (string s in storyboardEncoder.EncodeEnumerable(beatmap.Storyboard)) yield return s;
        yield return @"[TimingPoints]";
        foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints.Where(tp => tp != null)) {
            yield return timingPointEncoder.Encode(tp);
        }
        yield return @"";
        if (beatmap.ComboColoursList.Any()) {
            yield return @"";
            yield return @"[Colours]";
            foreach (string s in beatmap.ComboColoursList.Select((comboColour, i) =>
                         $"Combo{i + 1} : {ComboColour.SerializeComboColour(comboColour)}"))
                yield return s;
            foreach (string s in beatmap.SpecialColours.Select(specialColour => specialColour.Key + " : " +
                                                                                ComboColour.SerializeComboColour(specialColour.Value)))
                yield return s;
        }
        yield return @"";
        yield return @"[HitObjects]";
        foreach (HitObject ho in beatmap.HitObjects) {
            yield return hitObjectEncoder.Encode(ho);
        }
        yield return @"";
    }

    public string Encode(IBeatmap obj) {
        return string.Join(Environment.NewLine, EncodeEnumerable(obj));
    }
}