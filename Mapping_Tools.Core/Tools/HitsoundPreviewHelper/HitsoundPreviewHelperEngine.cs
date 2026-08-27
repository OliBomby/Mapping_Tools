using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Progress;

namespace Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

/// <summary>
///     Applies positional hitsound rules to a framework-independent beatmap.
/// </summary>
public static class HitsoundPreviewHelperEngine
{
    /// <summary>
    ///     Places the nearest configured zone's hitsound on every timeline event
    ///     belonging to one of the supplied hit objects.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap receiving the preview hitsounds.</param>
    /// <param name="selectedObjects">The hit objects whose timeline events are eligible.</param>
    /// <param name="zones">The non-empty positional rules to apply.</param>
    /// <param name="progress">Optional normalized progress for the selected events.</param>
    /// <param name="cancellationToken">Cancels before or during timeline mutation.</param>
    /// <returns>The number of timeline events updated.</returns>
    /// <exception cref="ArgumentException">No zone is supplied.</exception>
    public static int Apply(
        Beatmap beatmap,
        IReadOnlyCollection<HitObject> selectedObjects,
        IReadOnlyList<HitsoundZone> zones,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(selectedObjects);
        Validate(zones);

        var selected = selectedObjects.ToHashSet();
        List<TimelineObject> timelineObjects = beatmap.GetTimeline().TimelineObjects
            .Where(timelineObject => selected.Contains(timelineObject.Origin))
            .ToList();
        for (int index = 0; index < timelineObjects.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timelineObject = timelineObjects[index];
            var closest = zones[0];
            double closestDistance = double.MaxValue;
            foreach (var zone in zones)
            {
                double distance = zone.Distance(timelineObject.Origin.Pos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = zone;
                }
            }

            timelineObject.Filename = closest.Filename;
            timelineObject.SampleSet = closest.SampleSet;
            timelineObject.AdditionSet = closest.AdditionsSet;
            timelineObject.CustomIndex = closest.CustomIndex;
            timelineObject.SampleVolume = 0;
            timelineObject.SetHitsound(closest.Hitsound);
            timelineObject.HitsoundsToOrigin();
            progress?.Report(index + 1, timelineObjects.Count);
        }

        progress?.Report(1);
        return timelineObjects.Count;
    }

    /// <summary>Validates the configured positional hitsound rules.</summary>
    /// <param name="zones">The positional rules to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="zones" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">No zones exist, or a zone contains an undefined enum value.</exception>
    public static void Validate(IReadOnlyList<HitsoundZone> zones)
    {
        ArgumentNullException.ThrowIfNull(zones);
        if (zones.Count == 0) throw new ArgumentException("There are no zones!", nameof(zones));

        foreach (var zone in zones)
        {
            ArgumentNullException.ThrowIfNull(zone);
            if (!Enum.IsDefined(zone.Hitsound)
                || !Enum.IsDefined(zone.SampleSet)
                || !Enum.IsDefined(zone.AdditionsSet))
                throw new ArgumentException(
                    "Hitsound Preview Helper contains an unknown hitsound or sample-set value.",
                    nameof(zones));
        }
    }
}
