using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public static class Extensions {
        public static HitObject ToBeatmapHelperHitObject(this Editor_Reader.HitObject ob) {
            var ho = new HitObject {
                PixelLength = ob.SpatialLength,
                Time = ob.StartTime,
                ObjectType = ob.Type,
                EndTime = ob.EndTime,
                Hitsounds = ob.SoundType,
                Pos = new Vector2(ob.X, ob.Y),
                // Let the end position be the same as the start position before changed later for sliders
                EndPos = new Vector2(ob.X, ob.Y),
                Filename = ob.SampleFile,
                SampleVolume = ob.SampleVolume,
                SampleSet = (SampleSet) ob.SampleSet,
                AdditionSet = (SampleSet) ob.SampleSetAdditions,
                CustomIndex = ob.CustomSampleSet,
                IsSelected = ob.IsSelected
            };

            if (ho.IsSlider) {
                ho.Repeat = ob.SegmentCount;

                ho.SliderType = (PathType) ob.CurveType;
                if (ob.sliderCurvePoints != null) {
                    ho.CurvePoints = new List<Vector2>(ob.sliderCurvePoints.Length / 2);
                    for (var i = 1; i < ob.sliderCurvePoints.Length / 2; i++)
                        ho.CurvePoints.Add(new Vector2(ob.sliderCurvePoints[i * 2], ob.sliderCurvePoints[i * 2 + 1]));
                }

                ho.EdgeHitsounds = new List<int>(ho.Repeat + 1);
                if (ob.SoundTypeList != null)
                    ho.EdgeHitsounds = ob.SoundTypeList.ToList();
                for (var i = ho.EdgeHitsounds.Count; i < ho.Repeat + 1; i++) ho.EdgeHitsounds.Add(0);

                ho.EdgeSampleSets = new List<SampleSet>(ho.Repeat + 1);
                ho.EdgeAdditionSets = new List<SampleSet>(ho.Repeat + 1);
                if (ob.SampleSetList != null)
                    ho.EdgeSampleSets = Array.ConvertAll(ob.SampleSetList, ss => (SampleSet) ss).ToList();
                if (ob.SampleSetAdditionsList != null)
                    ho.EdgeAdditionSets = Array.ConvertAll(ob.SampleSetAdditionsList, ss => (SampleSet) ss).ToList();
                for (var i = ho.EdgeSampleSets.Count; i < ho.Repeat + 1; i++) ho.EdgeSampleSets.Add(SampleSet.Auto);
                for (var i = ho.EdgeAdditionSets.Count; i < ho.Repeat + 1; i++) ho.EdgeAdditionSets.Add(SampleSet.Auto);
            } else if (ho.IsSpinner || ho.IsHoldNote) {
                ho.Repeat = 1;
            } else {
                ho.Repeat = 0;
            }

            return ho;
        }

        public static TimingPoint ToBeatmapHelperTimingPoint(this Editor_Reader.ControlPoint cp) {
            var tp = new TimingPoint {
                MpB = cp.BeatLength,
                Offset = cp.Offset,
                SampleIndex = cp.CustomSamples,
                SampleSet = (SampleSet) cp.SampleSet,
                Meter = new TempoSignature(cp.TimeSignature),
                Volume = cp.Volume,
                Kiai = (cp.EffectFlags & 1) > 0,
                OmitFirstBarLine = (cp.EffectFlags & 8) > 0,
                Uninherited = cp.TimingChange
            };

            return tp;
        }
    }
}