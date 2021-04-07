namespace Mapping_Tools_Core.BeatmapHelper.Types {
    /// <summary>
    /// Indicates that the type has a duration.
    /// </summary>
    public interface IHasDuration {
        double Duration { get; }
        double EndTime { get; }
    }
}