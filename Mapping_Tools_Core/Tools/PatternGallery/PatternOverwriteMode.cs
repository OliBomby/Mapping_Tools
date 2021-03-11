namespace Mapping_Tools_Core.Tools.PatternGallery {
    public enum PatternOverwriteMode {
        /// <summary>
        /// Remove no objects from the original beatmap.
        /// </summary>
        NoOverwrite,
        /// <summary>
        /// Remove objects from the original beatmap only in dense parts of the pattern.
        /// </summary>
        PartitionedOverwrite,
        /// <summary>
        /// Remove all objects from the original beatmap between the start time of the pattern and the end time of the pattern.
        /// </summary>
        CompleteOverwrite,
    }
}