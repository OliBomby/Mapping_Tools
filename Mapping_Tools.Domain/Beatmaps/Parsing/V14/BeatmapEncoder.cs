using System.Text;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Timings;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class BeatmapEncoder(
    IEncoder<Storyboard> storyboardEncoder,
    IEncoder<HitObject> hitObjectEncoder,
    IEncoder<TimingPoint> timingPointEncoder,
    IEncoder<ComboColour> comboColourEncoder)
    : IEncoder<Beatmap> {
    public BeatmapEncoder() : this(
        new StoryboardEncoder(),
        new HitObjectEncoder(),
        new TimingPointEncoder(),
        new ComboColourEncoder()) {
    }

    public string Encode(Beatmap beatmap) {
        var builder = new StringBuilder();

        builder.AppendLine("osu file format v" + beatmap.BeatmapVersion.ToInvariant());
        builder.AppendLine();

        builder.AppendLine("[General]");
        builder.AppendLine("AudioFilename: " + beatmap.General.AudioFilename);
        builder.AppendLine("AudioLeadIn: " + beatmap.General.AudioLeadIn.ToInvariant());
        //builder.AppendLine("AudioHash: " + AudioEngine.AudioMd5);
        builder.AppendLine("PreviewTime: " + beatmap.General.PreviewTime.ToInvariant());
        builder.AppendLine("Countdown: " + ((int) beatmap.General.Countdown).ToInvariant());
        builder.AppendLine("SampleSet: " + beatmap.General.SampleSet);
        builder.AppendLine("StackLeniency: " + beatmap.General.StackLeniency.ToInvariant());
        builder.AppendLine("Mode: " + ((int) beatmap.General.Mode).ToInvariant());
        builder.AppendLine("LetterboxInBreaks: " + (beatmap.General.LetterboxInBreaks ? "1" : "0"));
        if (!beatmap.General.StoryFireInFront)
            builder.AppendLine("StoryFireInFront: 0");
        if (beatmap.General.UseSkinSprites)
            builder.AppendLine("UseSkinSprites: 1");
        if (beatmap.General.AlwaysShowPlayfield)
            builder.AppendLine("AlwaysShowPlayfield: 1");
        if (beatmap.General.OverlayPosition != OverlayPosition.NoChange)
            builder.AppendLine("OverlayPosition: " + beatmap.General.OverlayPosition);
        if (!string.IsNullOrEmpty(beatmap.General.SkinPreference))
            builder.AppendLine("SkinPreference:" + beatmap.General.SkinPreference);
        if (beatmap.General.EpilepsyWarning)
            builder.AppendLine("EpilepsyWarning: 1");
        if (beatmap.General.CountdownOffset > 0)
            builder.AppendLine("CountdownOffset: " + beatmap.General.CountdownOffset.ToInvariant());
        if (beatmap.General.Mode == GameMode.Mania)
            builder.AppendLine("SpecialStyle: " + (beatmap.General.SpecialStyle ? "1" : "0"));
        if (beatmap.BeatmapVersion > 10 || beatmap.General.WidescreenStoryboard)
            builder.AppendLine("WidescreenStoryboard: " + (beatmap.General.WidescreenStoryboard ? "1" : "0"));
        if (beatmap.General.SamplesMatchPlaybackRate)
            builder.AppendLine("SamplesMatchPlaybackRate: 1");
        builder.AppendLine();

        builder.AppendLine("[Editor]");

        if (beatmap.Editor.Bookmarks.Count > 0) {
            var bookmarksBuilder = new StringBuilder();
            bookmarksBuilder.AppendJoin(',', beatmap.Editor.Bookmarks.Select(b => b.ToRoundInvariant()));
            builder.AppendLine("Bookmarks: " + bookmarksBuilder);
        }

        builder.AppendLine("DistanceSpacing: " + beatmap.Editor.DistanceSpacing.ToInvariant());
        builder.AppendLine("BeatDivisor: " + beatmap.Editor.BeatDivisor.ToInvariant());
        builder.AppendLine("GridSize: " + beatmap.Editor.GridSize.ToInvariant());
        if (beatmap.BeatmapVersion > 10 || beatmap.Editor.TimelineZoom != 1f)
            builder.AppendLine("TimelineZoom: " + beatmap.Editor.TimelineZoom.ToInvariant());
        builder.AppendLine();

        builder.AppendLine("[Metadata]");
        builder.AppendLine("Title:" + beatmap.Metadata.Title);
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.TitleUnicode != beatmap.Metadata.Title)
            builder.AppendLine("TitleUnicode:" + beatmap.Metadata.TitleUnicode);
        builder.AppendLine("Artist:" + beatmap.Metadata.Artist);
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.ArtistUnicode != beatmap.Metadata.Artist)
            builder.AppendLine("ArtistUnicode:" + beatmap.Metadata.ArtistUnicode);
        builder.AppendLine("Creator:" + beatmap.Metadata.Creator);
        builder.AppendLine("Version:" + beatmap.Metadata.Version);
        builder.AppendLine("Source:" + beatmap.Metadata.Source);
        builder.AppendLine("Tags:" + beatmap.Metadata.Tags);
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.BeatmapId != 0)
            builder.AppendLine("BeatmapID:" + beatmap.Metadata.BeatmapId.ToInvariant());
        if (beatmap.BeatmapVersion > 9 || beatmap.Metadata.BeatmapSetId != -1)
            builder.AppendLine("BeatmapSetID:" + beatmap.Metadata.BeatmapSetId.ToInvariant());
        builder.AppendLine();

        builder.AppendLine("[Difficulty]");
        builder.AppendLine("HPDrainRate:" + beatmap.Difficulty.HpDrainRate.ToInvariant());
        builder.AppendLine("CircleSize:" + beatmap.Difficulty.CircleSize.ToInvariant());
        builder.AppendLine("OverallDifficulty:" + beatmap.Difficulty.OverallDifficulty.ToInvariant());
        builder.AppendLine("ApproachRate:" + beatmap.Difficulty.ApproachRate.ToInvariant());
        builder.AppendLine("SliderMultiplier:" + beatmap.Difficulty.SliderMultiplier.ToInvariant());
        builder.AppendLine("SliderTickRate:" + beatmap.Difficulty.SliderTickRate.ToInvariant());
        builder.AppendLine();

        builder.AppendLine(storyboardEncoder.Encode(beatmap.Storyboard));
        builder.AppendLine("[TimingPoints]");
        foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints) {
            builder.AppendLine(timingPointEncoder.Encode(tp));
        }

        builder.AppendLine();
        if (beatmap.ComboColoursList.Count != 0) {
            builder.AppendLine();
            builder.AppendLine("[Colours]");
            foreach (string s in beatmap.ComboColoursList.Select((comboColour, i) =>
                         $"Combo{i + 1} : {comboColourEncoder.Encode(comboColour)}"))
                builder.AppendLine(s);
            foreach (string s in beatmap.SpecialColours.Select(specialColour => specialColour.Key + " : " + comboColourEncoder.Encode(specialColour.Value)))
                builder.AppendLine(s);
        }

        builder.AppendLine();
        builder.AppendLine("[HitObjects]");
        foreach (HitObject ho in beatmap.HitObjects) {
            builder.AppendLine(hitObjectEncoder.Encode(ho));
        }

        builder.AppendLine();

        return builder.ToString();
    }
}