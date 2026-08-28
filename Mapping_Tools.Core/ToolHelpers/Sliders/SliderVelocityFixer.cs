using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders;

/// <summary>Rebuilds persisted slider-velocity timing after slider edits.</summary>
public static class SliderVelocityFixer
{
    /// <summary>
    ///     Writes the current slider velocities to inherited timing points and,
    ///     for marked sliders, optionally expresses their velocity through
    ///     temporary BPM redlines.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap whose slider timing is being rebuilt.</param>
    /// <param name="markedObjects">The sliders whose velocity may be delegated to BPM.</param>
    /// <param name="delegateToBpm">Whether marked slider velocity is expressed with BPM redlines.</param>
    /// <param name="removeSliderTicks">Whether delegated sliders use <see cref="double.NaN" /> SV to remove slider ticks.</param>
    /// <param name="cancellationToken">Cancels while iterating the beatmap's sliders.</param>
    public static void Fix(
        Beatmap beatmap,
        IReadOnlyCollection<HitObject> markedObjects,
        bool delegateToBpm,
        bool removeSliderTicks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(markedObjects);

        var timing = beatmap.BeatmapTiming;
        HashSet<HitObject> markedObjectSet = new(markedObjects);
        List<TimingPointChange> changes = [];
        foreach (var hitObject in beatmap.HitObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!hitObject.IsSlider) continue;

            if (markedObjectSet.Contains(hitObject) && delegateToBpm)
            {
                var redlineAfter = timing.GetRedlineAtTime(hitObject.Time).Copy();
                var redlineOn = redlineAfter.Copy();
                redlineAfter.Offset = hitObject.Time;
                redlineOn.Offset = hitObject.Time - 1;
                redlineAfter.OmitFirstBarLine = true;
                redlineOn.OmitFirstBarLine = true;
                redlineOn.MpB *= hitObject.SliderVelocity / -100;
                hitObject.SliderVelocity = removeSliderTicks ? double.NaN : -100;

                changes.Add(new TimingPointChange(
                    redlineOn,
                    true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DOUBLE_EPSILON));
                changes.Add(new TimingPointChange(
                    redlineAfter,
                    true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DOUBLE_EPSILON));
                hitObject.Time -= 1;
            }

            var sliderVelocity = timing.GetTimingPointAtTime(hitObject.Time).Copy();
            sliderVelocity.Offset = hitObject.Time;
            sliderVelocity.MpB = hitObject.SliderVelocity;
            changes.Add(new TimingPointChange(
                sliderVelocity,
                true,
                fuzziness: Precision.DOUBLE_EPSILON));
        }

        TimingPointChange.Apply(timing, changes);
    }
}
