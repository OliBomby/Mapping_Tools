using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>Applies Tumour Generator's legacy slider-velocity preservation rule.</summary>
public static class TumourSliderVelocityFixer
{
    /// <summary>
    ///     Rebuilds slider velocity timing changes after tumour paths change length.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap being generated.</param>
    /// <param name="markedObjects">The objects selected for this run.</param>
    /// <param name="delegateToBpm">Whether inherited velocity is expressed with BPM redlines.</param>
    /// <param name="removeSliderTicks">Whether delegated velocity removes slider ticks with NaN SV.</param>
    /// <param name="cancellationToken">Cancels while iterating hit objects.</param>
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
        List<TimingPointChange> changes = [];
        foreach (var hitObject in beatmap.HitObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!hitObject.IsSlider) continue;

            if (markedObjects.Contains(hitObject) && delegateToBpm)
            {
                var after = timing.GetRedlineAtTime(hitObject.Time).Copy();
                var before = after.Copy();
                after.Offset = hitObject.Time;
                before.Offset = hitObject.Time - 1;
                after.OmitFirstBarLine = true;
                before.OmitFirstBarLine = true;
                before.MpB *= hitObject.SliderVelocity / -100;
                hitObject.SliderVelocity = removeSliderTicks ? double.NaN : -100;

                changes.Add(new TimingPointChange(
                    before,
                    true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DOUBLE_EPSILON));
                changes.Add(new TimingPointChange(
                    after,
                    true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DOUBLE_EPSILON));
                hitObject.Time -= 1;
            }

            var sliderVelocity = hitObject.TimingPoint.Copy();
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
