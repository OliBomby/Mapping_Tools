using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper.ComboColours;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.Sections;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
/// Class containing all the data from a .osu beatmap file. It also supports serialization to .osu format and helper methods to get data in specific ways.
/// </summary>
public class Beatmap : IBeatmap {
    public int BeatmapVersion { get; set; }

    public BeatmapSetInfo BeatmapSet { get; set; }

    public SectionGeneral General { get; set; }

    public SectionEditor Editor { get; set; }

    public SectionMetadata Metadata { get; set; }

    public SectionDifficulty Difficulty { get; set; }

    public List<IComboColour> ComboColoursList { get; set; }

    IReadOnlyList<IComboColour> IHasComboColours.ComboColours => ComboColoursList;

    public Dictionary<string, IComboColour> SpecialColours { get; set; }

    public Timing BeatmapTiming { get; set; }

    public Storyboard Storyboard { get; set; }

    IStoryboard IBeatmap.Storyboard => Storyboard;

    /// <summary>
    /// List of all the hit objects in this beatmap.
    /// </summary>
    public List<HitObject> HitObjects { get; set; }

    /// <summary>
    /// Initializes a new Beatmap.
    /// </summary>
    public Beatmap() {
        General = new SectionGeneral();
        Editor = new SectionEditor();
        Metadata = new SectionMetadata();
        Difficulty = new SectionDifficulty();
        ComboColoursList = new List<IComboColour>();
        SpecialColours = new Dictionary<string, IComboColour>();
        Storyboard = new Storyboard();
        HitObjects = new List<HitObject>();
        BeatmapTiming = new Timing(1.4);
    }

    /// <summary>
    /// Initializes a beatmap with the provided hit objects and timing points.
    /// </summary>
    /// <param name="hitObjects"></param>
    /// <param name="timingPoints"></param>
    /// <param name="firstUnInheritedTimingPoint"></param>
    /// <param name="globalSv"></param>
    /// <param name="gameMode"></param>
    public Beatmap(List<HitObject> hitObjects, List<TimingPoint> timingPoints,
        TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) : this() {
        // Set the hit objects
        HitObjects = hitObjects;

        // Set the timing stuff
        BeatmapTiming.SetTimingPoints(timingPoints);
        BeatmapTiming.GlobalSliderMultiplier = globalSv;

        if (!BeatmapTiming.Contains(firstUnInheritedTimingPoint)) {
            BeatmapTiming.Add(firstUnInheritedTimingPoint);
        }

        // Set the global SV here too because thats absolutely necessary
        Difficulty.SliderMultiplier = globalSv;
        General.Mode = gameMode;

        this.SortHitObjects();
        this.GiveObjectsTimingContext();
        this.CalculateHitObjectComboStuff();
    }

    IBeatmap IBeatmap.Clone() => Clone();

    public Beatmap Clone() => (Beatmap) MemberwiseClone();

    IBeatmap IBeatmap.DeepClone() => DeepClone();

    public Beatmap DeepClone() {
        var newBeatmap = (Beatmap)MemberwiseClone();
        newBeatmap.HitObjects = HitObjects.Select(h => h.DeepClone()).ToList();
        newBeatmap.BeatmapTiming = new Timing(BeatmapTiming.TimingPoints.Select(t => t.Copy()).ToList(), BeatmapTiming.GlobalSliderMultiplier);
        newBeatmap.GiveObjectsTimingContext();
        return newBeatmap;
    }
}