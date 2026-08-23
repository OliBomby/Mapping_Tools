using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders;

namespace Mapping_Tools.Core.Tools.SliderCompletionator;

/// <summary>
/// Applies Slider Completionator's duration, length, velocity, and anchor edits
/// to a parsed beatmap without depending on an editor or frontend.
/// </summary>
public static class SliderCompletionatorEngine
{
    /// <summary>
    /// Completes the supplied sliders in place and reconstructs delegated timing points.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap being edited.</param>
    /// <param name="markedObjects">Objects selected by the Application import-mode logic.</param>
    /// <param name="options">The persisted completion settings.</param>
    /// <param name="currentEditorTime">
    /// The editor timestamp in milliseconds when current-editor-time mode is active.
    /// </param>
    /// <param name="progress">Optional progress receiver, reported as a percentage.</param>
    /// <param name="cancellationToken">Cancels between objects and before timing reconstruction.</param>
    /// <returns>The number of sliders changed.</returns>
    /// <exception cref="ArgumentException">The selected settings produce an invalid slider duration.</exception>
    public static int Apply(
        Beatmap beatmap,
        IReadOnlyList<HitObject> markedObjects,
        SliderCompletionatorOptions options,
        double? currentEditorTime = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(markedObjects);
        ArgumentNullException.ThrowIfNull(options);

        if (currentEditorTime is double time && !double.IsFinite(time))
        {
            throw new ArgumentException(
                "Current editor time must be a finite number.",
                nameof(currentEditorTime));
        }

        if (options.UseCurrentEditorTime && options.UseEndTime && currentEditorTime is null)
        {
            throw new ArgumentException(
                "Current editor time is required when current-editor-time mode is enabled.",
                nameof(currentEditorTime));
        }

        ValidateFiniteInputs(options);

        int slidersCompleted = 0;
        double endTime = options.UseCurrentEditorTime && options.UseEndTime
            ? currentEditorTime!.Value
            : options.EndTime;
        Timing timing = beatmap.BeatmapTiming;
        HashSet<HitObject> markedObjectSet = new(markedObjects);

        for (int i = 0; i < markedObjects.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HitObject hitObject = markedObjects[i];
            if (hitObject.IsSlider)
            {
                double millisecondsPerBeat = timing.GetMpBAtTime(hitObject.Time);
                double oldDuration = timing.CalculateSliderTemporalLength(
                    hitObject.Time,
                    hitObject.PixelLength);
                double oldLength = hitObject.PixelLength;
                double oldVelocity = timing.GetSvAtTime(hitObject.Time);

                double newDuration = options.UseEndTime
                    ? endTime == -1 && !options.UseCurrentEditorTime
                        ? oldDuration
                        : endTime - hitObject.Time
                    : options.Duration == -1
                        ? oldDuration
                        : timing.WalkBeatsInMillisecondTime(options.Duration, hitObject.Time) - hitObject.Time;
                double newLength = options.Length == -1
                    ? oldLength
                    : hitObject.GetSliderPath(fullLength: true).Distance * options.Length;
                double newVelocity = options.SliderVelocity == -1
                    ? oldVelocity
                    : -100 / options.SliderVelocity;

                switch (options.FreeVariableSetting)
                {
                    case SliderCompletionatorFreeVariable.Velocity:
                        newVelocity = -10000 * timing.SliderMultiplier * newDuration /
                                      (newLength * millisecondsPerBeat);
                        break;
                    case SliderCompletionatorFreeVariable.Duration:
                        // This actually doesn't get used anymore because the .osu doesn't store the duration
                        newDuration = newLength * newVelocity * millisecondsPerBeat /
                                      (-10000 * timing.SliderMultiplier);
                        break;
                    case SliderCompletionatorFreeVariable.Length:
                        newLength = -10000 * timing.SliderMultiplier * newDuration /
                                    (newVelocity * millisecondsPerBeat);
                        break;
                    default:
                        throw new ArgumentException("Unexpected free variable setting.", nameof(options));
                }

                if (!double.IsFinite(newLength) ||
                    !double.IsFinite(newVelocity))
                {
                    throw new ArgumentException(
                        "Encountered a non-finite slider value. Make sure none of the inputs are zero.",
                        nameof(options));
                }

                if (!double.IsFinite(newDuration))
                {
                    throw new ArgumentException(
                        "Encountered a non-finite slider duration. Make sure the inputs are finite.",
                        nameof(options));
                }

                if (newDuration < 0)
                {
                    throw new ArgumentException(
                        "Encountered slider with negative duration. Make sure the end time is greater than the end time of all selected sliders.",
                        nameof(options));
                }

                hitObject.SliderVelocity = newVelocity;
                hitObject.PixelLength = newLength;

                if (options.MoveAnchors)
                {
                    // Scale anchors to completion
                    hitObject.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                        hitObject.GetAllCurvePoints(),
                        hitObject.SliderType,
                        hitObject.PixelLength,
                        out PathType pathType));
                    hitObject.SliderType = pathType;
                }

                slidersCompleted++;
            }

            progress?.Report(markedObjects.Count == 0 ? 100 : (double)i / markedObjects.Count * 100);
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Reconstruct SliderVelocity
        List<TimingPointChange> changes = [];
        // Add Hitobject stuff
        foreach (HitObject hitObject in beatmap.HitObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!hitObject.IsSlider)
            {
                continue;
            }

            // SliderVelocity changes
            if (markedObjectSet.Contains(hitObject) && options.DelegateToBpm)
            {
                TimingPoint redlineAfter = timing.GetRedlineAtTime(hitObject.Time).Copy();
                TimingPoint redlineOn = redlineAfter.Copy();

                redlineAfter.Offset = hitObject.Time;
                redlineOn.Offset = hitObject.Time - 1; // This one will be on the slider
                redlineAfter.OmitFirstBarLine = true;
                redlineOn.OmitFirstBarLine = true;
                // Express velocity in BPM
                redlineOn.MpB *= hitObject.SliderVelocity / -100;
                // NaN SV results in removal of slider ticks
                hitObject.SliderVelocity = options.RemoveSliderTicks ? double.NaN : -100;

                // Add redlines
                changes.Add(new TimingPointChange(
                    redlineOn,
                    mpb: true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DoubleEpsilon));
                changes.Add(new TimingPointChange(
                    redlineAfter,
                    mpb: true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DoubleEpsilon));
                hitObject.Time -= 1;
            }

            TimingPoint timingPoint = hitObject.TimingPoint.Copy();
            timingPoint.Offset = hitObject.Time;
            timingPoint.MpB = hitObject.SliderVelocity;
            changes.Add(new TimingPointChange(
                timingPoint,
                mpb: true,
                fuzziness: Precision.DoubleEpsilon));
        }

        // Add the new SliderVelocity changes
        TimingPointChange.Apply(timing, changes);
        progress?.Report(100);
        return slidersCompleted;
    }

    private static void ValidateFiniteInputs(SliderCompletionatorOptions options)
    {
        if (!double.IsFinite(options.Duration) ||
            !double.IsFinite(options.EndTime) ||
            !double.IsFinite(options.Length) ||
            !double.IsFinite(options.SliderVelocity))
        {
            throw new ArgumentException(
                "Slider Completionator values must be finite numbers.",
                nameof(options));
        }
    }
}
