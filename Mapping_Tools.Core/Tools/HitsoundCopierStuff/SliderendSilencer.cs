using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers;

namespace Mapping_Tools.Core.Tools.HitsoundCopierStuff;

/// <summary>
/// Tool that mutes slider-ends and spinner-ends.
/// </summary>
public class SliderendSilencer {
    /// <summary>
    /// All the possible beat divisors a tick could be classified as.
    /// </summary>
    /// <remarks>
    /// This should be a superset of the <see cref="MutedDivisors"/>.
    /// </remarks>
    public IBeatDivisor[] BeatDivisors = {
        new RationalBeatDivisor(1),
        new RationalBeatDivisor(4), new RationalBeatDivisor(3),
        new RationalBeatDivisor(8), new RationalBeatDivisor(6),
        new RationalBeatDivisor(16), new RationalBeatDivisor(12)
    };

    /// <summary>
    /// All the beat divisors that should be muted.
    /// </summary>
    public IBeatDivisor[] MutedDivisors = {
        new RationalBeatDivisor(4), new RationalBeatDivisor(3),
        new RationalBeatDivisor(8), new RationalBeatDivisor(6),
        new RationalBeatDivisor(16), new RationalBeatDivisor(12)
    };

    /// <summary>
    /// The minimum number of beats a slider should have to be eligible for sliderend muting.
    /// </summary>
    public double MinLength = 0.5;

    /// <summary>
    /// The sample index to assign to sliderends that get muted.
    /// </summary>
    /// <remarks>
    /// If this value is -1, the sample index will just be inherited.
    /// </remarks>
    public int MutedIndex = -1;

    /// <summary>
    /// The sample set to assign to sliderends that get muted.
    /// </summary>
    /// <remarks>
    /// If this value is not None, sliderends that do not match this sample set will not get muted.
    /// </remarks>
    public SampleSet MutedSampleSet = SampleSet.None;

    /// <summary>
    /// Mutes slider-ends and spinner-ends in a beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to mute sliderends of.</param>
    /// <param name="processedTimeline">If provided, uses this as the timeline of the beatmap.
    /// Any timeline objects with <see cref="HasCopiedContext"/> will not be muted.</param>
    public void MuteSliderends(IBeatmap beatmap, Timeline processedTimeline = null) {
        var doMutedIndex = MutedIndex >= 0;

        beatmap.GiveObjectsTimingContext();
        processedTimeline ??= beatmap.GetTimeline();
        processedTimeline.GiveTimingContext(beatmap.BeatmapTiming);
            
        var controlChanges = new List<ControlChange>();
        foreach (var tloTo in processedTimeline.TimelineObjects) {
            if (FilterMuteTlo(tloTo, beatmap)) {
                // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
                tloTo.Hitsounds.SampleSet = MutedSampleSet;
                tloTo.Hitsounds.AdditionSet = 0;
                tloTo.Hitsounds.Normal = false;
                tloTo.Hitsounds.Whistle = false;
                tloTo.Hitsounds.Finish = false;
                tloTo.Hitsounds.Clap = false;
                    
                tloTo.HitsoundsToOrigin();

                // Add timingpointschange to copy timingpoint hitsounds
                var tp = tloTo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                tp.Offset = tloTo.Time;
                tp.SampleSet = MutedSampleSet;
                tp.SampleIndex = MutedIndex;
                tp.Volume = 5;
                controlChanges.Add(new ControlChange(tp, sampleset: true, index: doMutedIndex,
                    volume: true));
            } else {
                // Add timingpointschange to preserve index and volume and sampleset
                var tp = tloTo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                tp.Offset = tloTo.Time;
                controlChanges.Add(new ControlChange(tp, sampleset: true, index: doMutedIndex,
                    volume: true));
            }
        }

        // Apply the timingpoint changes
        ControlChange.ApplyChanges(beatmap.BeatmapTiming, controlChanges);
    }

    private bool FilterMuteTlo(TimelineObject tloTo, IBeatmap beatmap) {
        // Check whether this tlo has been copied to
        if (tloTo.HasContext<HasCopiedContext>())
            return false;

        // Check type
        if (tloTo is not (SliderTail or SpinnerTail))
            return false;

        // Make sure the slider is not a repeating slider
        if (tloTo is SliderTail sliderTail && sliderTail.NodeIndex != 1) {
            return false;
        }

        // Check if this tlo has hitsounds
        if (tloTo.Hitsounds.Whistle || tloTo.Hitsounds.Finish || tloTo.Hitsounds.Clap ||
            (MutedSampleSet != SampleSet.None && tloTo.FenoSampleSet != MutedSampleSet)) {
            return false;
        }

        // Check filter snap
        var allBeatDivisors = BeatDivisors;

        var timingPoint = beatmap.BeatmapTiming.GetRedlineAtTime(tloTo.Time - 1);
        var resnappedTime = beatmap.BeatmapTiming.Resnap(tloTo.Time, allBeatDivisors, false, tp: timingPoint);
        var beatsFromRedline = (resnappedTime - timingPoint.Offset) / timingPoint.MpB;

        // Get all the divisors which the sliderend could possibly be snapped to
        var possibleDivisors =
            allBeatDivisors.Where(d => Precision.AlmostEquals(beatsFromRedline % d.GetValue(), 0) ||
                                       Precision.AlmostEquals(beatsFromRedline % d.GetValue(), 1));

        // Make sure all the possible beat divisors of lower priority are in the muted category
        if (possibleDivisors.TakeWhile(d => !MutedDivisors.Contains(d)).Any()) {
            return false;
        }

        // Check filter minimum duration
        return tloTo.Origin == null || Precision.AlmostBigger(tloTo.Origin.Duration, MinLength * timingPoint.MpB);
    }
}