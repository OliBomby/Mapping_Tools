#nullable disable
namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
///     Expands hit objects into individually editable head, repeat, tail, and duration-edge events.
/// </summary>
public class Timeline
{
    /// <inheritdoc />
    public Timeline(List<HitObject> hitObjects, Timing timing)
    {
        // Convert all the HitObjects to TimeLineObjects
        TimelineObjects = new List<TimelineObject>();

        foreach (var ho in hitObjects)
        {
            ho.TimelineObjects = new List<TimelineObject>();
            if (ho.IsCircle)
            {
                TimelineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                ho.TimelineObjects.Add(TimelineObjects.Last());
            }
            else if (ho.IsSlider)
            {
                // Adding TimeLineObject for every repeat of the slider
                double sliderTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);

                for (int i = 0; i <= ho.Repeat; i++)
                {
                    double time = Math.Floor(ho.Time + sliderTemporalLength * i);
                    TimelineObjects.Add(new TimelineObject(ho, time, ho.ObjectType, i, ho.EdgeHitsounds[i], ho.EdgeSampleSets[i], ho.EdgeAdditionSets[i]));
                    ho.TimelineObjects.Add(TimelineObjects.Last());
                }
            }
            else if (ho.IsSpinner) // Only the end has hitsounds
            {
                TimelineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, 0, 0, 0));
                ho.TimelineObjects.Add(TimelineObjects.Last());
                TimelineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                ho.TimelineObjects.Add(TimelineObjects.Last());
            }
            else // Hold note. Only start has hitsounds
            {
                TimelineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                ho.TimelineObjects.Add(TimelineObjects.Last());
                TimelineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, 0, 0, 0));
                ho.TimelineObjects.Add(TimelineObjects.Last());
            }
        }

        // Sort the TimeLineObjects by their time
        TimelineObjects = TimelineObjects.OrderBy(o => o.Time).ToList();
    }

    /// <inheritdoc />
    public Timeline(List<TimelineObject> timeLineObjects)
    {
        TimelineObjects = timeLineObjects;
    }

    /// <summary>
    ///     Gets or sets expanded events in chronological order.
    /// </summary>
    public List<TimelineObject> TimelineObjects { get; set; }

    /// <summary>
    ///     Finds expanded events inside an inclusive millisecond range.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<TimelineObject> GetTimeLineObjectsInRange(double start, double end)
    {
        return TimelineObjects.FindAll(o => o.Time >= start && o.Time <= end);
    }

    /// <summary>
    ///     Resolves active timing and inherited sample settings for every expanded event.
    /// </summary>
    /// <param name="timing"></param>
    public void GiveTimingPoints(Timing timing)
    {
        foreach (var tlo in TimelineObjects)
        {
            var hstp = timing.GetTimingPointAtTime(tlo.Time + 5); // +5 for the weird offset in hitsounding greenlines
            tlo.GiveHitsoundTimingPoint(hstp);
            var tp = timing.GetTimingPointAtTime(tlo.Time);
            tlo.TimingPoint = tp;
            var red = timing.GetRedlineAtTime(tlo.Time);
            tlo.UninheritedTimingPoint = tp;
        }
    }

    /// <summary>
    ///     Finds the chronologically nearest expanded event, optionally requiring a copyable edge.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="needCopyable"></param>
    /// <returns></returns>
    public TimelineObject GetNearestTlo(double time, bool needCopyable = false)
    {
        if (TimelineObjects.Count == 0) return null;

        TimelineObject closest = null;
        double closestDist = double.PositiveInfinity;
        foreach (var tlo in TimelineObjects)
        {
            double dist = Math.Abs(tlo.Time - time);
            if (dist <= closestDist)
            {
                if (needCopyable && !(tlo.CanCopy && tlo.HasHitsound))
                    continue;
                closest = tlo;
                closestDist = dist;
            }
            else
            {
                return closest;
            }
        }

        return closest;
    }
}
