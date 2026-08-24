namespace Mapping_Tools.Core.Tools.PatternGallery.Models;

/// <summary>Chooses which target objects are removed before placement.</summary>
public enum PatternOverwriteMode
{
    /// <summary>Leaves all existing target objects in place.</summary>
    NoOverwrite,

    /// <summary>Removes existing objects only inside dense pattern partitions.</summary>
    PartitionedOverwrite,

    /// <summary>Removes existing objects throughout the pattern's time span.</summary>
    CompleteOverwrite,
}

