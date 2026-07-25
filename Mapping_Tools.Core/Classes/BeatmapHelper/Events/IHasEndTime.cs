namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Indicates that a type has an end time. Used by Property Transformer on Events
    /// </summary>
#nullable disable

    public interface IHasEndTime {
        /// <summary>
        /// Gets or sets the absolute end time in milliseconds.
        /// </summary>
        double EndTime { get; set; }
    }
}
