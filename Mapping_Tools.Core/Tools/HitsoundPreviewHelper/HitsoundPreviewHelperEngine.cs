using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.HitsoundStuff;

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
    /// <param name="progress">Optional percentage progress for the selected events.</param>
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
        ArgumentNullException.ThrowIfNull(zones);
        if (zones.Count == 0) throw new ArgumentException("There are no zones!", nameof(zones));

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
            progress?.Report(timelineObjects.Count == 0
                ? 100
                : (index + 1) * 100d / timelineObjects.Count);
        }

        progress?.Report(100);
        return timelineObjects.Count;
    }
}
