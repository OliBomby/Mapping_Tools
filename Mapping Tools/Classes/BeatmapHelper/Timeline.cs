using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class Timeline {
        public List<TimelineObject> TimeLineObjects { get; set; }

        public Timeline(List<HitObject> hitObjects, Timing timing) {
            // Convert all the HitObjects to TimeLineObjects
            TimeLineObjects = new List<TimelineObject>();

            foreach (HitObject ho in hitObjects) {
                ho.TimelineObjects = new List<TimelineObject>();
                if (ho.IsCircle) {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
                else if (ho.IsSlider) {
                    // Adding TimeLineObject for every repeat of the slider
                    double sliderTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);

                    for (int i = 0; i <= ho.Repeat; i++) {
                        double time = Math.Floor(ho.Time + sliderTemporalLength * i);
                        TimeLineObjects.Add(new TimelineObject(ho, time, ho.ObjectType, i, ho.EdgeHitsounds[i], ho.EdgeSampleSets[i], ho.EdgeAdditionSets[i]));
                        ho.TimelineObjects.Add(TimeLineObjects.Last());
                    }
                }
                else if (ho.IsSpinner) // Only the end has hitsounds
                {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, 0, 0, 0));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                    TimeLineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
                else // Hold note. Only start has hitsounds
                {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                    TimeLineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, 0, 0, 0));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
            }

            // Sort the TimeLineObjects by their time
            TimeLineObjects = TimeLineObjects.OrderBy(o => o.Time).ToList();
        }

        public Timeline(List<TimelineObject> timeLineObjects) {
            TimeLineObjects = timeLineObjects;
        }

        public List<TimelineObject> GetTimeLineObjectsInRange(double start, double end) {
            return TimeLineObjects.FindAll(o => o.Time >= start && o.Time <= end);
        }

        public void GiveTimingPoints(Timing timing) {
            foreach (TimelineObject tlo in TimeLineObjects) {
                TimingPoint hstp = timing.GetTimingPointAtTime(tlo.Time + 5); // +5 for the weird offset in hitsounding greenlines
                tlo.GiveHitsoundTimingPoint(hstp);
                TimingPoint tp = timing.GetTimingPointAtTime(tlo.Time);
                tlo.TP = tp;
                TimingPoint red = timing.GetRedlineAtTime(tlo.Time);
                tlo.Redline = tp;
            }
        }

        public TimelineObject GetNearestTLO(double time, bool needCopyable = false) {
            if (TimeLineObjects.Count == 0) {
                return null;
            }

            TimelineObject closest = null;
            double dist = double.PositiveInfinity;
            foreach (TimelineObject tlo in TimeLineObjects) {
                double d = Math.Abs(tlo.Time - time);
                if (d < dist) {
                    if (needCopyable && !tlo.canCopy)
                        continue;
                    closest = tlo;
                    dist = d;
                } else {
                    return closest;
                }
            }
            return closest;
        }
    }
}
