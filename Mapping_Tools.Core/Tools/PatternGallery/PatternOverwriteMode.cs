namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// How to merge a pattern with the destination beatmap.
/// </summary>
public enum PatternOverwriteMode {
    /// <summary>
    /// Remove no objects from the destination beatmap.
    /// </summary>
    NoOverwrite,
    /// <summary>
    /// Remove objects from the destination beatmap between the start and end times of parts of the pattern.
    /// </summary>
    PartitionedOverwrite,
    /// <summary>
    /// Remove all objects from the destination beatmap between the start and end time of the whole pattern.
    /// </summary>
    CompleteOverwrite,
}