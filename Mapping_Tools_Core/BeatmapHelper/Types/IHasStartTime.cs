namespace Mapping_Tools_Core.BeatmapHelper.Types {
    /// <summary>
    /// Indicates that a type has a start time.
    /// </summary>
    public interface IHasStartTime {
        double StartTime { get; set; }
    }
}