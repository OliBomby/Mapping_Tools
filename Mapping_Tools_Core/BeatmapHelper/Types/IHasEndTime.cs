namespace Mapping_Tools_Core.BeatmapHelper.Types {
    /// <summary>
    /// Indicates that a type has an end time.
    /// </summary>
    public interface IHasEndTime {
        double EndTime { get; set; }
    }
}