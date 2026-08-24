using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.MapCleaner;

/// <summary>Summarizes cleanup changes and their timeline positions.</summary>
/// <param name="ObjectsResnapped">The number of hit objects or ends moved to valid snaps.</param>
/// <param name="SamplesRemoved">The number of unused sample files moved to recovery.</param>
/// <param name="TimingPointsRemoved">The number of redundant timing points removed.</param>
/// <param name="TimingPointsAdded">The timestamps of newly introduced timing points.</param>
/// <param name="TimingPointsChanged">The timestamps of rebuilt timing points.</param>
/// <param name="TimingPointsRemovedAt">The timestamps from which timing points were removed.</param>
/// <param name="TimelineEndTime">The final timeline position needed to display all changes.</param>
public sealed record MapCleanerResult(
    int ObjectsResnapped,
    int SamplesRemoved,
    int TimingPointsRemoved,
    IReadOnlyList<double> TimingPointsAdded,
    IReadOnlyList<double> TimingPointsChanged,
    IReadOnlyList<double> TimingPointsRemovedAt,
    double TimelineEndTime)
{
    /// <summary>Combines this result with another beatmap's cleanup summary.</summary>
    /// <param name="other">The result to append.</param>
    /// <returns>A result containing summed counts, concatenated markers, and the later end time.</returns>
    public MapCleanerResult Add(MapCleanerResult other)
    {
        return new MapCleanerResult(
            ObjectsResnapped + other.ObjectsResnapped,
            SamplesRemoved + other.SamplesRemoved,
            TimingPointsRemoved + other.TimingPointsRemoved,
            TimingPointsAdded.Concat(other.TimingPointsAdded).ToArray(),
            TimingPointsChanged.Concat(other.TimingPointsChanged).ToArray(),
            TimingPointsRemovedAt.Concat(other.TimingPointsRemovedAt).ToArray(),
            Math.Max(TimelineEndTime, other.TimelineEndTime));
    }
}
