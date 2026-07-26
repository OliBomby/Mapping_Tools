namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events {
#nullable disable

    /// <summary>
    /// Marks an event whose active length can be read or changed.
    /// </summary>
    public interface IHasDuration {
        /// <summary>
        /// Gets or sets the event duration in milliseconds.
        /// </summary>
        double Duration { get; set; }
    }
}
