using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.MapCleaner;

public sealed class MapCleanerOptions
{
    public bool VolumeSliders { get; set; } = true;
    public bool SampleSetSliders { get; set; } = true;
    public bool VolumeSpinners { get; set; } = true;
    public bool ResnapObjects { get; set; } = true;
    public bool ResnapBookmarks { get; set; }
    public bool AnalyzeSamples { get; set; } = true;
    public bool RemoveUnusedSamples { get; set; }
    public bool RemoveHitsounds { get; set; }
    public bool RemoveMuting { get; set; }
    public bool RemoveUnclickableHitsounds { get; set; }
    public IBeatDivisor[] BeatDivisors { get; set; } = RationalBeatDivisor.GetDefaultBeatDivisors();
}

public sealed record MapCleanerResult(
    int ObjectsResnapped,
    int SamplesRemoved,
    int TimingPointsRemoved,
    IReadOnlyList<double> TimingPointsAdded,
    IReadOnlyList<double> TimingPointsChanged,
    IReadOnlyList<double> TimingPointsRemovedAt,
    double TimelineEndTime)
{
    public MapCleanerResult Add(MapCleanerResult other) => new(
        ObjectsResnapped + other.ObjectsResnapped,
        SamplesRemoved + other.SamplesRemoved,
        TimingPointsRemoved + other.TimingPointsRemoved,
        TimingPointsAdded.Concat(other.TimingPointsAdded).ToArray(),
        TimingPointsChanged.Concat(other.TimingPointsChanged).ToArray(),
        TimingPointsRemovedAt.Concat(other.TimingPointsRemovedAt).ToArray(),
        Math.Max(TimelineEndTime, other.TimelineEndTime));
}
