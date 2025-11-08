using System.Text.RegularExpressions;
using Mapping_Tools.Domain.Beatmaps.Contexts;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Sections;
using Mapping_Tools.Domain.Beatmaps.Timelines;
using Mapping_Tools.Domain.Beatmaps.Timings;
using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps;

/// <summary>
/// Class containing all the data from a .osu beatmap file. It also supports serialization to .osu format and helper methods to get data in specific ways.
/// </summary>
public class Beatmap {
    public int BeatmapVersion { get; set; }

    public SectionGeneral General { get; set; }

    public SectionEditor Editor { get; set; }

    public SectionMetadata Metadata { get; set; }

    public SectionDifficulty Difficulty { get; set; }

    public List<ComboColour> ComboColoursList { get; set; }

    public Dictionary<string, ComboColour> SpecialColours { get; set; }

    public Timing BeatmapTiming { get; set; }

    public Storyboard Storyboard { get; set; }

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
        ComboColoursList = [];
        SpecialColours = new Dictionary<string, ComboColour>();
        Storyboard = new Storyboard();
        HitObjects = [];
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
        TimingPoint? firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) : this() {
        // Set the hit objects
        HitObjects = hitObjects;

        // Set the timing stuff
        BeatmapTiming.SetTimingPoints(timingPoints);
        BeatmapTiming.GlobalSliderMultiplier = globalSv;

        if (firstUnInheritedTimingPoint is not null && !BeatmapTiming.Contains(firstUnInheritedTimingPoint)) {
            BeatmapTiming.Add(firstUnInheritedTimingPoint);
        }

        // Set the global SV here too because thats absolutely necessary
        Difficulty.SliderMultiplier = globalSv;
        General.Mode = gameMode;

        this.SortHitObjects();
        this.GiveObjectsTimingContext();
        this.CalculateHitObjectComboStuff();
    }

    public Beatmap Clone() => (Beatmap) MemberwiseClone();

    public Beatmap DeepClone() {
        var newBeatmap = (Beatmap)MemberwiseClone();
        newBeatmap.HitObjects = HitObjects.Select(h => h.DeepClone()).ToList();
        newBeatmap.BeatmapTiming = new Timing(BeatmapTiming.TimingPoints.Select(t => t.Copy()).ToList(), BeatmapTiming.GlobalSliderMultiplier);
        newBeatmap.GiveObjectsTimingContext();
        return newBeatmap;
    }

    /// <summary>
    /// Returns the 4 default combo colours of osu!
    /// </summary>
    /// <returns></returns>
    public static ComboColour[] GetDefaultComboColours() {
        return [
            new ComboColour(255, 192, 0),
            new ComboColour(0, 202, 0),
            new ComboColour(18, 124, 255),
            new ComboColour(242, 24, 57),
        ];
    }
}

public static class BeatmapExtensions {
    /// <summary>
    /// An offset which needs to be applied to old beatmaps (v4 and lower) to correct timing changes that were applied at a game client level.
    /// </summary>
    public static readonly int EarlyVersionTimingOffset = 24;

    /// <summary>
    /// Upgrades the beatmap to the latest stable beatmap file format version (V14).
    /// </summary>
    public static void UpgradeBeatmapVersion(this Beatmap beatmap) {
        if (beatmap.BeatmapVersion < 5) {
            beatmap.OffsetTime(EarlyVersionTimingOffset);
        }

        beatmap.BeatmapVersion = 14;
    }

    /// <summary>
    /// Sorts all hitobjects in map by order of time.
    /// </summary>
    public static void SortHitObjects(this Beatmap beatmap) {
        beatmap.HitObjects = beatmap.HitObjects.OrderBy(d => d, Comparer<HitObject>.Default).ToList();
    }

    /// <summary>
    /// Calculates the end position for all hit objects.
    /// WARNING: Slow!
    /// </summary>
    public static void CalculateEndPositions(this Beatmap beatmap) {
        CalculateEndPositions(beatmap.HitObjects);
    }

    /// <summary>
    /// Calculates the end position for all hit objects.
    /// WARNING: Slow!
    /// </summary>
    public static void CalculateEndPositions(this IEnumerable<HitObject> hitObjects) {
        foreach (var ho in hitObjects) {
            if (ho is Slider slider) {
                slider.RecalculateEndPosition();
            }
        }
    }

    /// <summary>
    /// Actual osu! stable code for calculating stacked positions of all hit objects.
    /// Make sure slider end positions are calculated before using this procedure.
    /// </summary>
    /// <param name="beatmap">The beatmap to update stacking context on.</param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="rounded">Whether to use a rounded stackOffset</param>
    public static void UpdateStacking(this Beatmap beatmap, int startIndex = 0, int endIndex = -1, bool rounded = false) {
        UpdateStacking(beatmap.HitObjects, beatmap.Difficulty.StackOffset, beatmap.General.StackLeniency,
            beatmap.Difficulty.ApproachTime, startIndex, endIndex, rounded);
    }

    /// <summary>
    /// Actual osu! stable code for calculating stacked positions of all hit objects.
    /// Make sure slider end positions are calculated before using this procedure.
    /// </summary>
    /// <param name="preEmpt">The approach time.</param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="rounded">Whether to use a rounded stackOffset.</param>
    /// <param name="hitObjects">The hit objects to update stacking context on.</param>
    /// <param name="stackOffset">The offset in X and Y osu! pixels between objects in a stack.</param>
    /// <param name="stackLeniency">The stacking leniency.</param>
    public static void UpdateStacking(this IList<HitObject> hitObjects, double stackOffset, double stackLeniency, double preEmpt, int startIndex = 0, int endIndex = -1,
        bool rounded = false) {
        if (endIndex == -1)
            endIndex = hitObjects.Count - 1;

        // Round the stack offset so objects only get offset by integer values
        if (rounded) {
            stackOffset = Math.Round(stackOffset);
        }

        const int stackLenience = 3;

        Vector2 stackVector = new Vector2(stackOffset, stackOffset);
        float stackThresold = (float) (preEmpt * stackLeniency);

        // Reset stacking inside the update range
        // Make sure stacking context exists for all objects
        for (int i = 0; i < hitObjects.Count; i++) {
            if (i >= startIndex && i <= endIndex || !hitObjects[i].HasContext<StackingContext>()) {
                hitObjects[i].SetContext(new StackingContext(stackVector));
            } else {
                hitObjects[i].GetContext<StackingContext>().StackVector = stackVector;
            }
        }

        // Extend the end index to include objects they are stacked on
        int extendedEndIndex = endIndex;
        for (int i = endIndex; i >= startIndex; i--) {
            int stackBaseIndex = i;
            for (int n = stackBaseIndex + 1; n < hitObjects.Count; n++) {
                HitObject stackBaseObject = hitObjects[stackBaseIndex];
                if (stackBaseObject is Spinner) break;

                HitObject objectN = hitObjects[n];
                if (objectN is Spinner) continue;

                if (objectN.StartTime - stackBaseObject.EndTime > stackThresold)
                    //We are no longer within stacking range of the next object.
                    break;

                if (Vector2.Distance(stackBaseObject.Pos, objectN.Pos) < stackLenience
                    || stackBaseObject is Slider && Vector2.Distance(stackBaseObject.EndPos, objectN.Pos) < stackLenience) {
                    stackBaseIndex = n;

                    // HitObjects after the specified update range haven't been reset yet
                    objectN.GetContext<StackingContext>().StackCount = 0;
                }
            }

            if (stackBaseIndex > extendedEndIndex) {
                extendedEndIndex = stackBaseIndex;
                if (extendedEndIndex == hitObjects.Count - 1)
                    break;
            }
        }

        //Reverse pass for stack calculation.
        int extendedStartIndex = startIndex;
        for (int i = extendedEndIndex; i > startIndex; i--) {
            int n = i;
            /* We should check every note which has not yet got a stack.
             * Consider the case we have two interwound stacks and this will make sense.
             *
             * o <-1      o <-2
             *  o <-3      o <-4
             *
             * We first process starting from 4 and handle 2,
             * then we come backwards on the i loop iteration until we reach 3 and handle 1.
             * 2 and 1 will be ignored in the i loop because they already have a stack value.
             */

            HitObject objectI = hitObjects[i];
            StackingContext stackingI = objectI.GetContext<StackingContext>();

            if (stackingI.StackCount != 0 || objectI is Spinner) continue;

            /* If this object is a hitcircle, then we enter this "special" case.
             * It either ends with a stack of hitcircles only, or a stack of hitcircles that are underneath a slider.
             * Any other case is handled by the "is Slider" code below this.
             */
            if (objectI is HitCircle) {
                while (--n >= 0) {
                    HitObject objectN = hitObjects[n];
                    StackingContext stackingN = objectN.GetContext<StackingContext>();

                    if (objectN is Spinner) continue;

                    if (objectI.StartTime - objectN.EndTime > stackThresold)
                        //We are no longer within stacking range of the previous object.
                        break;

                    // HitObjects before the specified update range haven't been reset yet
                    if (n < extendedStartIndex) {
                        objectN.GetContext<StackingContext>().StackCount = 0;
                        extendedStartIndex = n;
                    }

                    /* This is a special case where hticircles are moved DOWN and RIGHT (negative stacking) if they are under the *last* slider in a stacked pattern.
                     *    o==o <- slider is at original location
                     *        o <- hitCircle has stack of -1
                     *         o <- hitCircle has stack of -2
                     */
                    if (objectN is Slider && Vector2.Distance(objectN.EndPos, objectI.Pos) < stackLenience) {
                        int offset = stackingI.StackCount - stackingN.StackCount + 1;
                        for (int j = n + 1; j <= i; j++) {
                            //For each object which was declared under this slider, we will offset it to appear *below* the slider end (rather than above).
                            if (Vector2.Distance(objectN.EndPos, hitObjects[j].Pos) < stackLenience)
                                hitObjects[j].GetContext<StackingContext>().StackCount -= offset;
                        }

                        //We have hit a slider.  We should restart calculation using this as the new base.
                        //Breaking here will mean that the slider still has StackCount of 0, so will be handled in the i-outer-loop.
                        break;
                    }

                    if (Vector2.Distance(objectN.Pos, objectI.Pos) < stackLenience) {
                        //Keep processing as if there are no sliders.  If we come across a slider, this gets cancelled out.
                        //NOTE: Sliders with start positions stacking are a special case that is also handled here.

                        stackingN.StackCount = stackingI.StackCount + 1;
                        objectI = objectN;
                    }
                }
            } else if (objectI is Slider) {
                /* We have hit the first slider in a possible stack.
                 * From this point on, we ALWAYS stack positive regardless.
                 */
                while (--n >= startIndex) {
                    HitObject objectN = hitObjects[n];
                    StackingContext stackingN = objectN.GetContext<StackingContext>();

                    if (objectN is Spinner) continue;

                    if (objectI.StartTime - objectN.StartTime > stackThresold)
                        //We are no longer within stacking range of the previous object.
                        break;

                    if (Vector2.Distance(objectN.EndPos, objectI.Pos) < stackLenience) {
                        stackingN.StackCount = stackingI.StackCount + 1;
                        objectI = objectN;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculates the which hit objects actually have a new combo.
    /// Calculates the combo index and combo colours for each hit object.
    /// This includes cases where the previous hit object is a spinner or doesnt exist.
    /// </summary>
    public static void CalculateHitObjectComboStuff(this Beatmap beatmap) {
        CalculateHitObjectComboStuff(beatmap.HitObjects, beatmap.ComboColoursList.Count == 0 ? null : beatmap.ComboColoursList.ToArray(), beatmap.Storyboard.BreakPeriods);
    }

    /// <summary>
    /// Calculates the which hit objects actually have a new combo.
    /// Calculates the combo index and combo colours for each hit object.
    /// This includes cases where the previous hit object is a spinner or doesnt exist.
    /// </summary>
    public static void CalculateHitObjectComboStuff(this IEnumerable<HitObject> hitObjects, ComboColour[]? comboColours = null, ICollection<Break>? breakPeriods = null) {
        HitObject? previousHitObject = null;
        int colourIndex = 0;
        int comboIndex = 0;

        // If there are no combo colours use the default combo colours so the hitobjects still have something
        var actingComboColours = comboColours is null || comboColours.Length == 0
            ? Beatmap.GetDefaultComboColours()
            : comboColours;

        foreach (var hitObject in hitObjects) {
            var actualNewCombo = hitObject.IsActualNewCombo(previousHitObject, breakPeriods);

            if (actualNewCombo) {
                var colourIncrement = hitObject.ComboIncrement + hitObject.ComboSkip;

                colourIndex = MathHelper.Mod(colourIndex + colourIncrement, actingComboColours.Length);
                comboIndex = 1;
            } else {
                comboIndex++;
            }

            // Add the combo context
            hitObject.SetContext(new ComboContext(actualNewCombo, comboIndex, colourIndex, actingComboColours[colourIndex]));

            previousHitObject = hitObject;
        }
    }

    /// <summary>
    /// Fixes illegal break periods and automatically adds new break periods in gaps >= 5000 ms.
    /// Assumes end times are set for all hit objects.
    /// </summary>
    public static void FixBreakPeriods(this Beatmap beatmap) {
        const double maxMargin = 5000;
        const double minLeftMargin = 200;
        const double minBreakTime = 650;
        const double autoBreakGapSize = 5000;

        var approachTime = beatmap.Difficulty.ApproachTime;
        var newBreakPeriods = new List<Break>(beatmap.Storyboard.BreakPeriods.Count);

        // Add new break periods
        for (int i = 0; i < beatmap.HitObjects.Count - 1; i++) {
            var prev = beatmap.HitObjects[i];
            var next = beatmap.HitObjects[i + 1];

            var existingBreak = beatmap.Storyboard.BreakPeriods.FirstOrDefault(b => {
                var middle = (b.StartTime + b.EndTime) / 2;
                return middle >= prev.EndTime && middle <= next.StartTime;
            });

            if (existingBreak is not null) {
                // Fix existing break periods
                var middle = (existingBreak.StartTime + existingBreak.EndTime) / 2;
                // Get the distance to the end of the hit object to the left
                var leftMargin = middle - prev.EndTime;
                // Get the distance to the start of the hit object to the right
                var rightMargin = next.StartTime - middle;

                // Clamp the start and end time into legal bounds
                existingBreak.StartTime = MathHelper.Clamp(existingBreak.StartTime, middle - leftMargin + minLeftMargin, middle - leftMargin + maxMargin);
                existingBreak.EndTime = MathHelper.Clamp(existingBreak.EndTime, middle + rightMargin - maxMargin, middle + rightMargin - approachTime);

                // Remove the break period if too small
                if (!Precision.DefinitelySmaller(existingBreak.Duration, minBreakTime)) {
                    newBreakPeriods.Add(existingBreak);
                }
            } else if (!Precision.DefinitelySmaller(next.StartTime - prev.EndTime, autoBreakGapSize)) {
                // Add new break
                newBreakPeriods.Add(new Break {
                    StartTime = prev.EndTime + minLeftMargin,
                    EndTime = next.StartTime - approachTime,
                });
            }
        }

        newBreakPeriods.Sort();
        beatmap.Storyboard.BreakPeriods = newBreakPeriods;
    }

    /// <summary>
    /// Adjusts combo skip for all the hitobjects so colour index is correct.
    /// Assumes a <see cref="ComboContext"/> is present for all hit objects.
    /// </summary>
    public static void FixComboSkip(this Beatmap beatmap) {
        HitObject previousHitObject = null;
        int colourIndex = 0;

        // If there are no combo colours use the default combo colours so the hitobjects still have something
        var actingComboColours = beatmap.ComboColoursList.Count == 0
            ? Beatmap.GetDefaultComboColours()
            : beatmap.ComboColoursList.ToArray();

        foreach (var hitObject in beatmap.HitObjects) {
            bool newCombo = hitObject.IsActualNewCombo(previousHitObject, beatmap.Storyboard.BreakPeriods);

            if (newCombo) {
                int colourIncrement = hitObject.ComboIncrement;
                var newColourIndex = MathHelper.Mod(colourIndex + colourIncrement, actingComboColours.Length);
                var wantedColourIndex = hitObject.GetContext<ComboContext>().ColourIndex;
                var diff = wantedColourIndex - newColourIndex;

                if (diff > 0) {
                    hitObject.ComboSkip = diff;
                } else if (diff < 0) {
                    hitObject.ComboSkip = actingComboColours.Length + diff;
                }

                int newColourIncrement = hitObject.ComboIncrement + hitObject.ComboSkip;
                colourIndex = MathHelper.Mod(colourIndex + newColourIncrement, actingComboColours.Length);
            }

            previousHitObject = hitObject;
        }
    }

    /// <summary>
    /// For each hit object it stores the timingpoints from <see cref="Beatmap.BeatmapTiming"/> which are affecting that hit object.
    /// Basically making all hit objects independent of <see cref="Beatmap.BeatmapTiming"/>.
    /// </summary>
    public static void GiveObjectsTimingContext(this Beatmap beatmap) {
        GiveObjectsTimingContext(beatmap.HitObjects, beatmap.BeatmapTiming);
    }

    /// <summary>
    /// For each hit object it stores the timingpoints from <see cref="Beatmap.BeatmapTiming"/> which are affecting that hit object.
    /// Basically making all hit objects independent of <see cref="Beatmap.BeatmapTiming"/>.
    /// </summary>
    public static void GiveObjectsTimingContext(this IEnumerable<HitObject> hitObjects, Timing timing) {
        foreach (var ho in hitObjects) {
            ho.SetContext(new TimingContext(timing.GlobalSliderMultiplier,
                timing.GetSvAtTime(ho.StartTime),
                timing.GetTimingPointAtTime(ho.StartTime),
                timing.GetTimingPointAtTime(ho.StartTime + 5),
                timing.GetRedlineAtTime(ho.StartTime)));

            // This has to be set afterwards because the EndTime is inaccessible before the hitobject has a timing context
            ho.GetContext<TimingContext>().BodyHitsounds =
                timing.GetTimingPointsInRange(ho.StartTime, ho.EndTime, false).Select(o => o.Copy()).ToList();
        }
    }

    /// <summary>
    /// Finds all hit objects from this beatmap which are within a specified range.
    /// Just any part of the hit object has to overlap with the time range in order to be included.
    /// </summary>
    /// <param name="beatmap">The beatmap to get hit objects from.</param>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <returns>All <see cref="HitObject"/> that are found within specified range.</returns>
    public static List<HitObject> GetHitObjectsWithRangeInRange(this Beatmap beatmap, double start, double end) {
        return beatmap.HitObjects.FindAll(o => o.EndTime >= start && o.StartTime <= end);
    }

    /// <summary>
    /// Finds all hit objects from this beatmap which are within a specified range.
    /// The entire hit object has to be inside the time range in order to be included.
    /// </summary>
    /// <param name="beatmap">The beatmap to get hit objects from.</param>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <returns>All <see cref="HitObject"/> that are found within specified range.</returns>
    public static List<HitObject> GetHitObjectsInRange(this Beatmap beatmap, double start, double end) {
        return beatmap.HitObjects.FindAll(o => o.StartTime >= start && o.EndTime <= end);
    }

    /// <summary>
    /// Creates a new <see cref="Timeline"/> for this Beatmap.
    /// Upon creation the timeline is updated with all the current timing and hitsounds of this beatmap,
    /// but later changes wont be automatically synchronized.
    /// This will also set the the <see cref="TimelineContext"/> of all hit objects which implement <see cref="IHasTimelineObjects"/>.
    /// </summary>
    /// <returns></returns>
    public static Timeline GetTimeline(this Beatmap beatmap) {
        Timeline tl = new Timeline(beatmap.HitObjects);
        tl.GiveTimingContext(beatmap.BeatmapTiming);
        return tl;
    }

    /// <summary>
    /// Returns all hit objects that have a bookmark in their range.
    /// </summary>
    /// <returns>A list of hit objects that have a bookmark in their range.</returns>
    public static List<HitObject> GetBookmarkedObjects(this Beatmap beatmap) {
        List<double> bookmarks = beatmap.Editor.Bookmarks;
        List<HitObject> markedObjects = beatmap.HitObjects.FindAll(ho => bookmarks.Exists(o => ho.StartTime <= o && o <= ho.EndTime));
        return markedObjects;
    }

    public static double GetHitObjectStartTime(this Beatmap beatmap) {
        return beatmap.HitObjects.Min(h => h.StartTime);
    }

    public static double GetHitObjectEndTime(this Beatmap beatmap) {
        return beatmap.HitObjects.Max(h => h.EndTime);
    }

    public static void OffsetTime(this Beatmap beatmap, double offset) {
        beatmap.General.PreviewTime = beatmap.General.PreviewTime == -1 ? -1 : beatmap.General.PreviewTime + EarlyVersionTimingOffset;

        foreach (var breakPeriod in beatmap.Storyboard.BreakPeriods) {
            breakPeriod.StartTime += EarlyVersionTimingOffset;
            breakPeriod.EndTime += EarlyVersionTimingOffset;
        }

        beatmap.BeatmapTiming.Offset(offset);
        beatmap.HitObjects.ForEach(h => h.MoveTime(offset));
    }

    public static double GetLeadInTime(this Beatmap beatmap) {
        double leadInTime = beatmap.General.AudioLeadIn;
        var od = beatmap.Difficulty.OverallDifficulty;
        var window50 = Math.Ceiling(200 - 10 * od);
        var eventsWithStartTime = beatmap.Storyboard.EnumerateAllEvents().OfType<IHasStartTime>().ToArray();
        if (eventsWithStartTime.Length > 0)
            leadInTime = Math.Max(-eventsWithStartTime.Min(o => o.StartTime), leadInTime);
        if (beatmap.HitObjects.Count > 0) {
            var approachTime = beatmap.Difficulty.ApproachTime;
            leadInTime = Math.Max(approachTime - beatmap.HitObjects[0].StartTime, leadInTime);
        }

        return leadInTime + window50 + 1000;
    }

    public static double GetMapStartTime(this Beatmap beatmap) {
        return -beatmap.GetLeadInTime();
    }

    public static double GetMapEndTime(this Beatmap beatmap) {
        var endTime = beatmap.HitObjects.Count > 0
            ? Math.Max(beatmap.GetHitObjectEndTime() + 200, beatmap.HitObjects.Last().EndTime + 3000)
            : double.NegativeInfinity;
        var eventsWithEndTime = beatmap.Storyboard.EnumerateAllEvents().OfType<IHasDuration>().ToArray();
        if (eventsWithEndTime.Length > 0)
            endTime = Math.Max(endTime, eventsWithEndTime.Max(o => o.EndTime) - 500);
        return endTime;
    }

    /// <summary>
    /// Gets the time at which auto-fail gets checked by osu!
    /// The counted judgements must add up to the object count at this time.
    /// </summary>
    /// <returns></returns>
    public static double GetAutoFailCheckTime(this Beatmap beatmap) {
        return beatmap.GetHitObjectEndTime() + 200;
    }

    /// <summary>
    /// Finds the objects referred by specified time code.
    /// </summary>
    /// <example>Example time code: 00:56:823 (1,2,1,2) - </example>
    /// <param name="beatmap">The beatmap to query from.</param>
    /// <param name="code">The time code.</param>
    /// <returns></returns>
    public static IEnumerable<HitObject> QueryTimeCode(this Beatmap beatmap, string code) {
        // Parse the time span in the code
        (TimeSpan timestampTime, List<HitObjectReference> hitObjectReferences) = TimestampParser.ParseTimestamp(code);

        // Enumerate through the hit objects from the first object at the time
        int objectIndex = beatmap.HitObjects.FindIndex(h => h.StartTime >= timestampTime.TotalMilliseconds);

        foreach (var hitObjectReference in hitObjectReferences) {
            if (hitObjectReference.ComboIndex.HasValue) {
                // Find by combo index
                int comboNumber = hitObjectReference.ComboIndex!.Value;

                while (comboNumber != -1 && objectIndex < beatmap.HitObjects.Count && beatmap.HitObjects[objectIndex].GetContext<ComboContext>().ComboIndex != comboNumber) {
                    objectIndex++;
                }

                if (objectIndex < beatmap.HitObjects.Count && objectIndex > 0)
                    yield return beatmap.HitObjects[objectIndex++];
            } else {
                // Find mania time and column index
                int maniaColumnIndex = hitObjectReference.ColumnIndex!.Value;
                int time = hitObjectReference.Time!.Value;

                var result = beatmap.HitObjects.FirstOrDefault(o =>
                    Math.Abs(o.StartTime - time) < 0.5 && beatmap.GetColumnIndex(o.Pos.X) == maniaColumnIndex);

                if (result is not null)
                    yield return result;
            }
        }
    }

    public static int GetColumnIndex(this Beatmap beatmap, double x) {
        int columnCount = (int)beatmap.Difficulty.CircleSize;
        double columnWidth = 512.0 / columnCount;
        int columnIndex = (int)(x / columnWidth);
        return MathHelper.Clamp(columnIndex, 0, columnCount - 1);
    }

    /// <summary>
    /// Grabs the specified file name of beatmap file.
    /// with format of:
    /// <c>Artist - Title (Host) [Difficulty].osu</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public static string GetFileName(this Beatmap beatmap) {
        return GetFileName(beatmap.Metadata.Artist, beatmap.Metadata.Title, beatmap.Metadata.Creator, beatmap.Metadata.Version);
    }

    /// <summary>
    /// Grabs the specified file name of beatmap file.
    /// with format of:
    /// <c>Artist - Title (Host) [Difficulty].osu</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public static string GetFileName(string artist, string title, string creator, string version) {
        string fileName = $"{artist} - {title} ({creator}) [{version}].osu";

        string regexSearch = new(Path.GetInvalidFileNameChars());
        Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
        fileName = r.Replace(fileName, "");
        return fileName;
    }
}