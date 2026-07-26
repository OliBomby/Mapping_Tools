namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Indicates that a type has a start time. Used by Property Transformer on Events
    /// </summary>
#nullable disable

    public interface IHasStartTime {
        /// <summary>
        /// Gets or sets the absolute start time in milliseconds.
        /// </summary>
        double StartTime { get; set; }
    }
}
