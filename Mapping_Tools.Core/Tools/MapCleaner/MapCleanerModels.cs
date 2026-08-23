using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.MapCleaner;

/// <summary>Defines the framework-independent cleanup operations performed by Map Cleaner.</summary>
public sealed class MapCleanerOptions
{
    /// <summary>Gets or sets whether slider volume changes must be preserved.</summary>
    public bool VolumeSliders { get; set; } = true;

    /// <summary>Gets or sets whether slider sample-set changes must be preserved.</summary>
    public bool SampleSetSliders { get; set; } = true;

    /// <summary>Gets or sets whether spinner volume changes must be preserved.</summary>
    public bool VolumeSpinners { get; set; } = true;

    /// <summary>Gets or sets whether hit objects and slider ends are resnapped.</summary>
    public bool ResnapObjects { get; set; } = true;

    /// <summary>Gets or sets whether editor bookmarks are resnapped.</summary>
    public bool ResnapBookmarks { get; set; }

    /// <summary>Gets or sets whether mapset samples are inspected before rebuilding timing points.</summary>
    public bool AnalyzeSamples { get; set; } = true;

    /// <summary>Gets or sets whether unused samples are moved to recoverable storage.</summary>
    public bool RemoveUnusedSamples { get; set; }

    /// <summary>Gets or sets whether every object hitsound is cleared.</summary>
    public bool RemoveHitsounds { get; set; }

    /// <summary>Gets or sets whether muting volume values are removed from object ends.</summary>
    public bool RemoveMuting { get; set; }

    /// <summary>Gets or sets whether unclickable slider and spinner ends are muted.</summary>
    public bool RemoveUnclickableHitsounds { get; set; }

    /// <summary>Gets or sets the beat divisors accepted while resnapping.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } = RationalBeatDivisor.GetDefaultBeatDivisors();
}

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
