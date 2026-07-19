namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Indicates that a type has a start time. Used by Property Transformer on Events
    /// </summary>
#nullable disable

    public interface IHasStartTime {
        double StartTime { get; set; }
    }
}
