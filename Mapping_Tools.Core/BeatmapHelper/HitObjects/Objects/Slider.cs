using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

public class Slider : HitObject, IRepeats, IHasTimelineObjects {
    public PathType SliderType { get; set; }

    [NotNull]
    public List<Vector2> CurvePoints { get; set; }

    public int RepeatCount { get; set; }
    public double PixelLength { get; set; }

    [NotNull]
    public List<HitSampleInfo> EdgeHitsounds { get; set; }

    public int SpanCount => RepeatCount + 1;

    public override double EndTime => StartTime + Duration;

    public override double Duration => SpanDuration * SpanCount;

    public override Vector2 EndPos => GetEndPosition();

    public double SpanDuration => GetSpanDuration();

    /// <summary>
    /// Cache for end position.
    /// </summary>
    private Vector2? endPos;

    public Slider() {
        CurvePoints = new List<Vector2>();
        EdgeHitsounds = new List<HitSampleInfo>();
    }

    public void SetDuration(double duration) {
        SetSpanDurationByPixelLength(duration / SpanCount);
    }

    public void SetEndTime(double newEndTime) {
        SetDuration(newEndTime - StartTime);
    }

    public void SetRepeatCount(int repeatCount) {
        RepeatCount = repeatCount;
    }

    public void SetSpanCount(int spanCount) {
        RepeatCount = spanCount - 1;
    }

    public void SetSpanDuration(double spanDuration) {
        SetSpanDurationByPixelLength(spanDuration);
    }

    /// <summary>
    /// Calculates repeat duration based on the timing context.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If no timing context was found in this hit object.</exception>
    /// <returns>The duration of one repeat in milliseconds.</returns>
    public double GetSpanDuration() {
        if (double.IsNaN(PixelLength) || PixelLength < 0 || CurvePoints.All(o => o == Pos)) {
            return 0;
        }

        var timing = GetContext<TimingContext>();
        return SvHelper.CalculateSliderDuration(PixelLength, timing.UninheritedTimingPoint.MpB, timing.SliderVelocity, timing.GlobalSliderVelocity);
    }

    /// <summary>
    /// Whether the slider extras should be written in the .osu file.
    /// </summary>
    public bool NeedSliderExtras() {
        return EdgeHitsounds.Any(o => !o.Equals(Hitsounds)) ||
               !Hitsounds.Equals(new HitSampleInfo());
    }

    /// <summary>
    /// Gets the end position of the slider path. Might be slow.
    /// If it's outdated use <see cref="RecalculateEndPosition"/>.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetEndPosition() {
        if (!endPos.HasValue) {
            RecalculateEndPosition();
        }

        Debug.Assert(endPos != null, nameof(endPos) + " != null");
        return endPos.Value;
    }

    /// <summary>
    /// Recalculates the <see cref="endPos"/> from the control points and pixel length.
    /// </summary>
    public void RecalculateEndPosition() {
        endPos = GetSliderPath().PositionAt(1);
    }

    public IEnumerable<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
        if (!TryGetContext<TimingContext>(out var timing)) {
            throw new InvalidOperationException("Slider is not initialized with timing context. Can not get the playing body filenames.");
        }

        // Get sliderslide hitsounds for every timingpoint in the slider
        if (includeDefaults || timing.TimingPoint.SampleIndex != 0) {
            var firstSampleSet = Hitsounds.SampleSet == SampleSet.None ? timing.TimingPoint.SampleSet : Hitsounds.SampleSet;
            yield return GetSliderFilename(firstSampleSet, "slide", timing.TimingPoint.SampleIndex);
            if (Hitsounds.Whistle)
                yield return GetSliderFilename(firstSampleSet, "whistle", timing.TimingPoint.SampleIndex);
        }

        foreach (var bodyTp in timing.BodyHitsounds)
            if (includeDefaults || bodyTp.SampleIndex != 0) {
                var sampleSet = Hitsounds.SampleSet == SampleSet.None ? bodyTp.SampleSet : Hitsounds.SampleSet;
                yield return GetSliderFilename(sampleSet, "slide", bodyTp.SampleIndex);
                if (Hitsounds.Whistle)
                    yield return GetSliderFilename(sampleSet, "whistle", bodyTp.SampleIndex);
            }

        // Add tick samples
        // 10 ms over tick time is tick
        var t = StartTime + timing.UninheritedTimingPoint.MpB / sliderTickRate;
        while (t + 10 < EndTime) {
            var bodyTp = Timing.GetTimingPointAtTime(t, timing.BodyHitsounds, timing.TimingPoint);
            if (includeDefaults || bodyTp.SampleIndex != 0) {
                var sampleSet = Hitsounds.SampleSet == SampleSet.None ? bodyTp.SampleSet : Hitsounds.SampleSet;
                yield return GetSliderFilename(sampleSet, "tick", bodyTp.SampleIndex);
            }

            t += timing.UninheritedTimingPoint.MpB / sliderTickRate;
        }
    }

    private string GetSliderFilename(SampleSet sampleSet, string sampleName, int index) {
        if (index == 0) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}-default.wav";
        if (index == 1) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}.wav";
        return $"{sampleSet.ToString().ToLower()}-slider{sampleName}{index}.wav";
    }


    /// <summary>
    /// Retrieves the hitsounds at a particular edge of the repeating slider.
    /// </summary>
    /// <param name="nodeIndex">The edge to attempt to retrieve the hitsounds at.</param>
    /// <returns>The samples at the given edge index, or the slider's default hitsounds if the given edge doesn't exist.</returns>
    public HitSampleInfo GetNodeSamples(int nodeIndex)
        => nodeIndex < EdgeHitsounds.Count ? EdgeHitsounds[nodeIndex] : Hitsounds;

    public override void ResetHitsounds() {
        for (int i = 0; i < EdgeHitsounds.Count; i++) {
            EdgeHitsounds[i] = new HitSampleInfo();
        }

        // TODO: check references to and manually remove or move timing context body hitsounds

        base.ResetHitsounds();
    }

    /// <summary>
    /// Sets the actual end time of the slider by changing the <see cref="PixelLength"/>.
    /// </summary>
    /// <param name="endTime">The new end time in milliseconds.</param>
    public void SetEndTimeByPixelLength(double endTime) {
        SetSpanDurationByPixelLength((endTime - StartTime) / SpanCount);
    }

    /// <summary>
    /// Sets the actual end time of the slider by changing the <see cref="PixelLength"/>.
    /// </summary>
    /// <param name="endTime">The new end time in milliseconds.</param>
    public void SetEndTimeBySliderVelocity (double endTime) {
        SetSpanDurationBySliderVelocity((endTime - StartTime) / SpanCount);
    }

    /// <summary>
    /// Sets the actual duration of the slider by changing the <see cref="PixelLength"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If no timing context was found in this hit object.</exception>
    /// <param name="duration">The new duration duration.</param>
    public void SetSpanDurationByPixelLength(double duration) {
        var timing = GetContext<TimingContext>();

        // Change the pixel length to match the new time
        PixelLength = SvHelper.CalculatePixelLength(duration, timing.UninheritedTimingPoint.MpB, timing.SliderVelocity, timing.GlobalSliderVelocity);
    }

    /// <summary>
    /// Sets the actual duration of the slider by changing the <see cref="TimingContext.SliderVelocity"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If no timing context was found in this hit object.</exception>
    /// <param name="duration">The new duration duration.</param>
    public void SetSpanDurationBySliderVelocity(double duration) {
        var timing = GetContext<TimingContext>();

        // Change the pixel length to match the new time
        timing.SliderVelocity = SvHelper.CalculateSliderVelocity(PixelLength, timing.UninheritedTimingPoint.MpB, duration, timing.GlobalSliderVelocity);
    }

    public override void Move(Vector2 delta) {
        base.Move(delta);
        for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = CurvePoints[i] + delta;
    }

    public override void Transform(Matrix2 mat) {
        base.Transform(mat);
        for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = Matrix2.Mult(mat, CurvePoints[i]);
    }

    public SliderPath GetSliderPath(bool fullLength = false) {
        return fullLength
            ? new SliderPath(SliderType, GetAllCurvePoints().ToArray())
            : new SliderPath(SliderType, GetAllCurvePoints().ToArray(), PixelLength);
    }

    public void SetSliderPath(SliderPath sliderPath) {
        var controlPoints = sliderPath.ControlPoints;
        SetAllCurvePoints(controlPoints);
        SliderType = sliderPath.Type;
        PixelLength = sliderPath.Distance;
    }

    public List<Vector2> GetAllCurvePoints() {
        var controlPoints = new List<Vector2> { Pos };
        controlPoints.AddRange(CurvePoints);
        return controlPoints;
    }

    public void SetAllCurvePoints(List<Vector2> controlPoints) {
        Pos = controlPoints.First();
        CurvePoints = controlPoints.GetRange(1, controlPoints.Count - 1);
    }

    /// <summary>
    /// Detects a failure in the slider path algorithm causing a slider to become invisible.
    /// </summary>
    /// <returns></returns>
    public bool IsInvisible() {
        return PixelLength != 0 && PixelLength <= 0.0001 ||
               double.IsNaN(PixelLength) ||
               CurvePoints.All(o => o == Pos);
    }

    protected override void DeepCloneAdd(HitObject clonedHitObject) {
        var slider = (Slider) clonedHitObject;

        slider.CurvePoints = CurvePoints.ToList();
        slider.EdgeHitsounds = EdgeHitsounds.Select(o => o.Clone()).ToList();
    }

    public IEnumerable<TimelineObject> GetTimelineObjects() {
        var context = new TimelineContext();
        // Adding TimeLineObject for every edge of the slider
        for (int i = 0; i < SpanCount + 1; i++) {
            double time = Math.Floor(StartTime + SpanDuration * i);
            var tlo = new SliderNode(time, GetNodeSamples(i).Clone(), i) { Origin = this };
            context.TimelineObjects.Add(tlo);
            yield return tlo;
        }
        SetContext(context);
    }

    private TimelineObject createTimelineObject(double time, HitSampleInfo hitsounds, int nodeIndex) {
        if (nodeIndex == 0) {
            return new SliderHead(time, GetNodeSamples(nodeIndex).Clone(), nodeIndex) {Origin = this};
        }

        if (nodeIndex == SpanCount) {
            return new SliderTail(time, GetNodeSamples(nodeIndex).Clone(), nodeIndex) {Origin = this};
        }

        return new SliderNode(time, GetNodeSamples(nodeIndex).Clone(), nodeIndex) {Origin = this};
    }
}