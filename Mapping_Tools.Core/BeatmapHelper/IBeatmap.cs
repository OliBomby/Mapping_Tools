using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper.ComboColours;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.Sections;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.BeatmapHelper;

public interface IBeatmap : IHasComboColours {
    /// <summary>
    /// The version number of the beatmap.
    /// <para/>
    /// Version 4 introduces custom samplesets per timing section.<br/>
    /// Version 5 changes the map's offset by 24ms due to an internal calculation change.<br/>
    /// Version 6 changes stacking algorithm and fixes animation speeds for storyboarded sprites.<br/>
    /// Version 7 fixes multipart bezier slider math error (http://osu.sifterapp.com/projects/4151/issues/145)<br/>
    /// Version 8 mm additions: constant sliderticks-per-beat; HP drain changes near breaks; Taiko triple drumrolls.<br/>
    /// Version 9 makes bezier the default slider type, which now handles linear corners better;
    /// Spinner new combos are no longer forced. (Some restrictions re-imposed by the editor.)<br/>
    /// Version 10 fixes sliders being 1/50 shorter than they should be for every bezier part.<br/>
    /// Version 11 Support hold notes.<br/>
    /// Version 14 Support per-node samplesets on sliders (ctb)<br/>
    /// </summary>
    int BeatmapVersion { get; set; }

    /// <summary>
    /// Information about the beatmap set this map is part of.
    /// </summary>
    [CanBeNull]
    [JsonIgnore]
    public BeatmapSetInfo BeatmapSet { get; set; }

    /// <summary>
    /// Contains all the values in the [General] section of a .osu file.
    /// </summary>
    [NotNull]
    SectionGeneral General { get; set; }

    /// <summary>
    /// Contains all the values in the [Editor] section of a .osu file.
    /// </summary>
    [NotNull]
    SectionEditor Editor { get; set; }

    /// <summary>
    /// Contains all the values in the [Metadata] section of a .osu file.
    /// </summary>
    [NotNull]
    SectionMetadata Metadata { get; set; }

    /// <summary>
    /// Contains all the values in the [Difficulty] section of a .osu file.
    /// </summary>
    [NotNull]
    SectionDifficulty Difficulty { get; set; }

    /// <summary>
    /// Contains all the basic combo colours. The order of this list is the same as how they are numbered in the .osu.
    /// There can not be more than 8 combo colours.
    /// <c>Combo1 : 245,222,139</c>
    /// </summary>
    [NotNull]
    List<IComboColour> ComboColoursList { get; set; }

    /// <summary>
    /// Contains all the special colours. These include the colours of slider bodies or slider outlines.
    /// The key is the name of the special colour and the value is the actual colour.
    /// </summary>
    [NotNull]
    Dictionary<string, IComboColour> SpecialColours { get; set; }

    /// <summary>
    /// The timing of this beatmap. This objects contains all the timing points (data from the [TimingPoints] section) plus the global slider multiplier.
    /// It also has a number of helper methods to fetch data from the timing points.
    /// With this object you can always calculate the slider velocity at any time.
    /// Any changes to the slider multiplier property in this object will not be serialized. Change the value in <see cref="Difficulty"/> instead.
    /// </summary>
    [NotNull]
    Timing BeatmapTiming { get; set; }

    /// <summary>
    /// The storyboard of the Beatmap. Stores everything under the [Events] section.
    /// </summary>
    [NotNull]
    IStoryboard Storyboard { get; }

    /// <summary>
    /// List of all the hit objects in this beatmap.
    /// </summary>
    [NotNull]
    List<HitObject> HitObjects { get; set; }

    /// <summary>
    /// Creates a deep-clone of this beatmap and returns it.
    /// </summary>
    /// <returns>The deep-cloned beatmap</returns>
    IBeatmap DeepClone();

    /// <summary>
    /// Creates a shallow-clone of this beatmap and returns it.
    /// </summary>
    /// <returns>The shallow-cloned beatmap</returns>
    IBeatmap Clone();
}

public static class IBeatmapExtensions {
    /// <summary>
    /// An offset which needs to be applied to old beatmaps (v4 and lower) to correct timing changes that were applied at a game client level.
    /// </summary>
    public static readonly int EarlyVersionTimingOffset = 24;

    /// <summary>
    /// Upgrades the beatmap to the latest stable beatmap file format version (V14).
    /// </summary>
    public static void UpgradeBeatmapVersion(this IBeatmap beatmap) {
        if (beatmap.BeatmapVersion < 5) {
            beatmap.OffsetTime(EarlyVersionTimingOffset);
        }

        beatmap.BeatmapVersion = 14;
    }

    /// <summary>
    /// Sorts all hitobjects in map by order of time.
    /// </summary>
    public static void SortHitObjects(this IBeatmap beatmap) {
        beatmap.HitObjects = beatmap.HitObjects.OrderBy(d => d, Comparer<HitObject>.Default).ToList();
    }

    /// <summary>
    /// Calculates the end position for all hit objects.
    /// WARNING: Slow!
    /// </summary>
    public static void CalculateEndPositions(this IBeatmap beatmap) {
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
    public static void UpdateStacking(this IBeatmap beatmap, int startIndex = 0, int endIndex = -1, bool rounded = false) {
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
    public static void UpdateStacking(this IList<HitObject> hitObjects, double stackOffset, double stackLeniency, double preEmpt, int startIndex = 0, int endIndex = -1, bool rounded = false) {
        if (endIndex == -1)
            endIndex = hitObjects.Count - 1;

        // Round the stack offset so objects only get offset by integer values
        if (rounded) {
            stackOffset = Math.Round(stackOffset);
        }

        const int stackLenience = 3;

        Vector2 stackVector = new Vector2(stackOffset, stackOffset);
        float stackThresold = (float)(preEmpt * stackLeniency);

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

                if (Vector2.Distance(stackBaseObject.Pos, objectN.Pos) < stackLenience ||
                    (stackBaseObject is Slider && Vector2.Distance(stackBaseObject.EndPos, objectN.Pos) < stackLenience)) {
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
    public static void CalculateHitObjectComboStuff(this IBeatmap beatmap) {
        CalculateHitObjectComboStuff(beatmap.HitObjects, beatmap.ComboColoursList.Count == 0 ? null : beatmap.ComboColoursList.ToArray(), beatmap.Storyboard.BreakPeriods);
    }

    /// <summary>
    /// Calculates the which hit objects actually have a new combo.
    /// Calculates the combo index and combo colours for each hit object.
    /// This includes cases where the previous hit object is a spinner or doesnt exist.
    /// </summary>
    public static void CalculateHitObjectComboStuff(this IEnumerable<HitObject> hitObjects, [CanBeNull] IComboColour[] comboColours = null, [CanBeNull] ICollection<Break> breakPeriods = null) {
        HitObject previousHitObject = null;
        int colourIndex = 0;
        int comboIndex = 0;

        // If there are no combo colours use the default combo colours so the hitobjects still have something
        var actingComboColours = comboColours is null || comboColours.Length == 0
            ? ComboColour.GetDefaultComboColours()
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
    public static void FixBreakPeriods(this IBeatmap beatmap) {
        const double maxMargin = 5000;
        const double minLeftMargin = 200;
        const double minBreakTime = 650;
        const double autoBreakGapSize = 5000;

        var approachTime = beatmap.Difficulty.ApproachTime;
        var newBreakPeriods = new List<Events.Break>(beatmap.Storyboard.BreakPeriods.Count);

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
                newBreakPeriods.Add(new Events.Break(prev.EndTime + minLeftMargin, next.StartTime - approachTime));
            }
        }

        newBreakPeriods.Sort();
        beatmap.Storyboard.BreakPeriods = newBreakPeriods;
    }

    /// <summary>
    /// Adjusts combo skip for all the hitobjects so colour index is correct.
    /// Assumes a <see cref="ComboContext"/> is present for all hit objects.
    /// </summary>
    public static void FixComboSkip(this IBeatmap beatmap) {
        HitObject previousHitObject = null;
        int colourIndex = 0;

        // If there are no combo colours use the default combo colours so the hitobjects still have something
        var actingComboColours = beatmap.ComboColoursList.Count == 0
            ? ComboColour.GetDefaultComboColours()
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
                    hitObject.ComboSkip = (actingComboColours.Length + diff);
                }

                int newColourIncrement = hitObject.ComboIncrement + hitObject.ComboSkip;
                colourIndex = MathHelper.Mod(colourIndex + newColourIncrement, actingComboColours.Length);
            }

            previousHitObject = hitObject;
        }
    }

    /// <summary>
    /// For each hit object it stores the timingpoints from <see cref="IBeatmap.BeatmapTiming"/> which are affecting that hit object.
    /// Basically making all hit objects independent of <see cref="IBeatmap.BeatmapTiming"/>.
    /// </summary>
    public static void GiveObjectsTimingContext(this IBeatmap beatmap) {
        GiveObjectsTimingContext(beatmap.HitObjects, beatmap.BeatmapTiming);
    }

    /// <summary>
    /// For each hit object it stores the timingpoints from <see cref="IBeatmap.BeatmapTiming"/> which are affecting that hit object.
    /// Basically making all hit objects independent of <see cref="IBeatmap.BeatmapTiming"/>.
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
    public static List<HitObject> GetHitObjectsWithRangeInRange(this IBeatmap beatmap, double start, double end) {
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
    public static List<HitObject> GetHitObjectsInRange(this IBeatmap beatmap, double start, double end) {
        return beatmap.HitObjects.FindAll(o => o.StartTime >= start && o.EndTime <= end);
    }

    /// <summary>
    /// Creates a new <see cref="Timeline"/> for this Beatmap.
    /// Upon creation the timeline is updated with all the current timing and hitsounds of this beatmap,
    /// but later changes wont be automatically synchronized.
    /// This will also set the the <see cref="TimelineContext"/> of all hit objects which implement <see cref="IHasTimelineObjects"/>.
    /// </summary>
    /// <returns></returns>
    public static Timeline GetTimeline(this IBeatmap beatmap) {
        Timeline tl = new Timeline(beatmap.HitObjects);
        tl.GiveTimingContext(beatmap.BeatmapTiming);
        return tl;
    }

    /// <summary>
    /// Returns all hit objects that have a bookmark in their range.
    /// </summary>
    /// <returns>A list of hit objects that have a bookmark in their range.</returns>
    public static List<HitObject> GetBookmarkedObjects(this IBeatmap beatmap) {
        List<double> bookmarks = beatmap.Editor.Bookmarks;
        List<HitObject> markedObjects = beatmap.HitObjects.FindAll(ho => bookmarks.Exists(o => (ho.StartTime <= o && o <= ho.EndTime)));
        return markedObjects;
    }

    public static double GetHitObjectStartTime(this IBeatmap beatmap) {
        return beatmap.HitObjects.Min(h => h.StartTime);
    }

    public static double GetHitObjectEndTime(this IBeatmap beatmap) {
        return beatmap.HitObjects.Max(h => h.EndTime);
    }

    public static void OffsetTime(this IBeatmap beatmap, double offset) {
        beatmap.General.PreviewTime = beatmap.General.PreviewTime == -1 ? -1 : beatmap.General.PreviewTime + EarlyVersionTimingOffset;

        foreach(var breakPeriod in beatmap.Storyboard.BreakPeriods) {
            breakPeriod.StartTime += EarlyVersionTimingOffset;
            breakPeriod.EndTime += EarlyVersionTimingOffset;
        }

        beatmap.BeatmapTiming.Offset(offset);
        beatmap.HitObjects.ForEach(h => h.MoveTime(offset));
    }

    public static double GetLeadInTime(this IBeatmap beatmap) {
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

    public static double GetMapStartTime(this IBeatmap beatmap) {
        return -beatmap.GetLeadInTime();
    }

    public static double GetMapEndTime(this IBeatmap beatmap) {
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
    public static double GetAutoFailCheckTime(this IBeatmap beatmap) {
        return beatmap.GetHitObjectEndTime() + 200;
    }

    /// <summary>
    /// Finds the objects refered by specified time code.
    /// </summary>
    /// <example>Example time code: 00:56:823 (1,2,1,2) - </example>
    /// <param name="beatmap">The beatmap to query from.</param>
    /// <param name="code">The time code.</param>
    /// <returns></returns>
    public static IEnumerable<HitObject> QueryTimeCode(this IBeatmap beatmap, string code) {
        var startBracketIndex = code.IndexOf("(", StringComparison.Ordinal);
        var endBracketIndex = code.IndexOf(")", StringComparison.Ordinal);

        // Extract the list of combo numbers from the code
        IEnumerable<int> comboNumbers;
        if (startBracketIndex == -1) {
            // If there is not start bracket, then we assume that there is no list of combo numbers in the code
            // -1 means just get any combo number
            comboNumbers = new[] { -1 };
        } else {
            if (endBracketIndex == -1) {
                endBracketIndex = code.Length - 1;
            }

            // Get the part of the code between the brackets
            var comboNumbersString = code.Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);

            comboNumbers = comboNumbersString.Split(',').Select(int.Parse);
        }

        // Parse the time span in the code
        var time = InputParsers.ParseOsuTimestamp(code).TotalMilliseconds;

        // Enumerate through the hit objects from the first object at the time
        int objectIndex = beatmap.HitObjects.FindIndex(h => h.StartTime >= time);

        if (objectIndex < 0) {
            yield break;
        }

        foreach (var comboNumber in comboNumbers) {
            while (comboNumber != -1 && objectIndex < beatmap.HitObjects.Count &&
                   beatmap.HitObjects[objectIndex].GetContext<ComboContext>().ComboIndex != comboNumber) {
                objectIndex++;
            }

            if (objectIndex >= beatmap.HitObjects.Count)
                yield break;

            yield return beatmap.HitObjects[objectIndex++];
        }
    }

    /// <summary>
    /// Gets the relative path to this beatmap in the beatmap set.
    /// Returns null if the beatmap has no beatmap set or the beatmap set doesn't have this beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to get the path of.</param>
    /// <returns>The path or null.</returns>
    public static string GetBeatmapSetRelativePath(this IBeatmap beatmap) {
        return beatmap.BeatmapSet?.GetRelativePath(beatmap);
    }

    /// <summary>
    /// Grabs the specified file name of beatmap file.
    /// with format of:
    /// <c>Artist - Title (Host) [Difficulty].osu</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public static string GetFileName(this IBeatmap beatmap) {
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

        string regexSearch = new string(Path.GetInvalidFileNameChars());
        Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
        fileName = r.Replace(fileName, "");
        return fileName;
    }
}