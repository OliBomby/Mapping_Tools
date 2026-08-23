using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.RhythmGuide;

/// <summary>Creates reference hit objects from expanded beatmap timelines.</summary>
public static class RhythmGuideGenerator
{
    /// <summary>Creates a new guide map by retaining the first source's base metadata and redlines.</summary>
    /// <param name="sources">The beatmaps whose expanded rhythm events are copied.</param>
    /// <param name="options">The output mode, name, selection, and snapping choices.</param>
    /// <param name="cancellationToken">Cancels timeline expansion or guide generation.</param>
    /// <returns>A new beatmap containing generated guide objects.</returns>
    public static Beatmap CreateNewMap(
        IReadOnlyList<Beatmap> sources,
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(options);
        if (sources.Count == 0) throw new ArgumentException("There must be at least one beatmap.", nameof(sources));

        // Scuffed beatmap copy
        Beatmap result = new(sources[0].GetLines());
        // Remove all greenlines
        result.BeatmapTiming.RemoveAll(point => !point.Uninherited);
        // Remove all hitobjects
        result.HitObjects.Clear();
        // Change some parameters;
        result.General["StackLeniency"] = new TValue("0.0");
        result.General["Mode"] = new TValue(((int)options.OutputGameMode).ToString());
        result.Metadata["Version"] = new TValue(options.OutputName);
        result.Difficulty["CircleSize"] = new TValue("4");
        // Add hitobjects
        Append(result, sources, options, cancellationToken);
        return result;
    }

    /// <summary>Appends guide objects to an existing target without changing its existing content.</summary>
    /// <param name="target">The beatmap that receives guide objects.</param>
    /// <param name="sources">The beatmaps whose expanded rhythm events are copied.</param>
    /// <param name="options">The selection, snapping, and new-combo choices.</param>
    /// <param name="cancellationToken">Cancels timeline expansion or guide generation.</param>
    public static void Append(
        Beatmap target,
        IEnumerable<Beatmap> sources,
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.BeatDivisors);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var timelineObject in source.GetTimeline().TimelineObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Handle different selection modes
                switch (options.SelectionMode)
                {
                    case RhythmGuideSelectionMode.AllEvents:
                        AddHitObject(timelineObject.Time);
                        break;
                    case RhythmGuideSelectionMode.HitsoundEvents:
                        if (timelineObject.HasHitsound) AddHitObject(timelineObject.Time);
                        break;
                    case RhythmGuideSelectionMode.AllEventSeparated:
                        bool active = timelineObject.IsHoldnoteHead || timelineObject.IsCircle || timelineObject.IsSliderHead;
                        AddHitObject(
                            timelineObject.Time,
                            active ? new Vector2(0, 192) : new Vector2(512, 192));
                        break;
                    case RhythmGuideSelectionMode.LongNotes:
                        bool startsObject = timelineObject.IsHoldnoteHead || timelineObject.IsCircle || timelineObject.IsSliderHead || timelineObject.IsSpinnerHead;
                        if (startsObject)
                        {
                            AddHitObject(timelineObject.Time);
                        }
                        else if (target.HitObjects.Count > 0)
                        {
                            // Extend last object
                            var last = target.HitObjects[^1];
                            last.IsCircle = false;
                            last.IsHoldNote = true;
                            last.EndTime = timelineObject.Time;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(options.SelectionMode),
                            options.SelectionMode,
                            "Unknown Rhythm Guide selection mode.");
                }
            }
        }

        void AddHitObject(double time, Vector2? position = null)
        {
            // Preserve the accepted legacy output: resnap is evaluated but the original event time is emitted.
            _ = target.BeatmapTiming.Resnap(time, options.BeatDivisors);
            HitObject hitObject = new(time, 0, SampleSet.None, SampleSet.None)
            {
                NewCombo = options.NcEverything,
            };
            if (position.HasValue) hitObject.Pos = position.Value;
            target.HitObjects.Add(hitObject);
        }
    }
}
