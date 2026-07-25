namespace Mapping_Tools.Classes.BeatmapHelper.Events {
#nullable disable

    /// <summary>
    /// Controls how an animated storyboard sprite cycles through its frames.
    /// </summary>
    public enum LoopType {
        /// <summary>
        /// Restarts at the first frame after the final frame.
        /// </summary>
        LoopForever,
        /// <summary>
        /// Stops after displaying the final frame once.
        /// </summary>
        LoopOnce,
    }
}
