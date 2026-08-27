using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;

namespace Mapping_Tools.Core.Tools.SliderCompletionator;

/// <summary>
///     Applies Slider Completionator's duration, length, velocity, and anchor edits
///     to a parsed beatmap without depending on an editor or frontend.
/// </summary>
public static class SliderCompletionatorEngine
{
    /// <summary>
    ///     Completes the supplied sliders in place and reconstructs delegated timing points.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap being edited.</param>
    /// <param name="markedObjects">Objects selected by the Application import-mode logic.</param>
    /// <param name="options">The persisted completion settings.</param>
    /// <param name="currentEditorTime">
    ///     The editor timestamp in milliseconds when current-editor-time mode is active.
    /// </param>
    /// <param name="progress">Optional normalized progress receiver.</param>
    /// <param name="cancellationToken">Cancels between objects and before timing reconstruction.</param>
    /// <returns>The number of sliders changed.</returns>
    /// <exception cref="ArgumentException">The selected settings produce an invalid slider duration.</exception>
    public static int Apply(
        Beatmap beatmap,
        IReadOnlyList<HitObject> markedObjects,
        SliderCompletionatorEngineOptions options,
        double? currentEditorTime = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(markedObjects);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        if (currentEditorTime is { } time && !double.IsFinite(time))
            throw new ArgumentException(
                "Current editor time must be a finite number.",
                nameof(currentEditorTime));

        if (options.UseCurrentEditorTime && options.UseEndTime && currentEditorTime is null)
            throw new ArgumentException(
                "Current editor time is required when current-editor-time mode is enabled.",
                nameof(currentEditorTime));

        int slidersCompleted = 0;
        double endTime = options.UseCurrentEditorTime && options.UseEndTime
            ? currentEditorTime!.Value
            : options.EndTime;
        var timing = beatmap.BeatmapTiming;
        HashSet<HitObject> markedObjectSet = new(markedObjects);

        for (int i = 0; i < markedObjects.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hitObject = markedObjects[i];
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
                    : hitObject.GetSliderPath(true).Distance * options.Length;
                double newVelocity = options.SliderVelocity == -1
                    ? oldVelocity
                    : -100 / options.SliderVelocity;

                switch (options.FreeVariableSetting)
                {
                    case SliderCompletionatorFreeVariable.Velocity:
                        newVelocity = -10000 * timing.SliderMultiplier * newDuration / (newLength * millisecondsPerBeat);
                        break;
                    case SliderCompletionatorFreeVariable.Duration:
                        // This actually doesn't get used anymore because the .osu doesn't store the duration
                        newDuration = newLength * newVelocity * millisecondsPerBeat / (-10000 * timing.SliderMultiplier);
                        break;
                    case SliderCompletionatorFreeVariable.Length:
                        newLength = -10000 * timing.SliderMultiplier * newDuration / (newVelocity * millisecondsPerBeat);
                        break;
                    default:
                        throw new ArgumentException("Unexpected free variable setting.", nameof(options));
                }

                if (!double.IsFinite(newLength) || !double.IsFinite(newVelocity))
                    throw new ArgumentException(
                        "Encountered a non-finite slider value. Make sure none of the inputs are zero.",
                        nameof(options));

                if (!double.IsFinite(newDuration))
                    throw new ArgumentException(
                        "Encountered a non-finite slider duration. Make sure the inputs are finite.",
                        nameof(options));

                if (newDuration < 0)
                    throw new ArgumentException(
                        "Encountered slider with negative duration. Make sure the end time is greater than the end time of all selected sliders.",
                        nameof(options));

                hitObject.SliderVelocity = newVelocity;
                hitObject.PixelLength = newLength;

                if (options.MoveAnchors)
                {
                    // Scale anchors to completion
                    hitObject.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                        hitObject.GetAllCurvePoints(),
                        hitObject.SliderType,
                        hitObject.PixelLength,
                        out var pathType));
                    hitObject.SliderType = pathType;
                }

                slidersCompleted++;
            }

            progress?.Report(i, markedObjects.Count);
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Reconstruct SliderVelocity
        List<TimingPointChange> changes = [];
        // Add Hitobject stuff
        foreach (var hitObject in beatmap.HitObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!hitObject.IsSlider) continue;

            // SliderVelocity changes
            if (markedObjectSet.Contains(hitObject) && options.DelegateToBpm)
            {
                var redlineAfter = timing.GetRedlineAtTime(hitObject.Time).Copy();
                var redlineOn = redlineAfter.Copy();

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

            var timingPoint = hitObject.TimingPoint.Copy();
            timingPoint.Offset = hitObject.Time;
            timingPoint.MpB = hitObject.SliderVelocity;
            changes.Add(new TimingPointChange(
                timingPoint,
                true,
                fuzziness: Precision.DOUBLE_EPSILON));
        }

        // Add the new SliderVelocity changes
        TimingPointChange.Apply(timing, changes);
        progress?.Report(1);
        return slidersCompleted;
    }

    /// <summary>Validates the free-variable mode and persisted numeric inputs.</summary>
    /// <param name="options">The Slider Completionator settings to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The free-variable mode is undefined or an input is non-finite.</exception>
    public static void Validate(SliderCompletionatorEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.FreeVariableSetting))
            throw new ArgumentException(
                "Slider Completionator contains an unknown free-variable mode.",
                nameof(options));
        if (!double.IsFinite(options.Duration) || !double.IsFinite(options.EndTime) || !double.IsFinite(options.Length) || !double.IsFinite(options.SliderVelocity))
            throw new ArgumentException(
                "Slider Completionator values must be finite numbers.",
                nameof(options));
    }
}
