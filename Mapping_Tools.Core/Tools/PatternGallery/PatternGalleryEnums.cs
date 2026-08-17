namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>Chooses the time reference used when placing a pattern.</summary>
public enum ExportTimeMode
{
    /// <summary>Places the pattern at the time encoded by its first object.</summary>
    Pattern,

    /// <summary>Places the first object at a caller-supplied millisecond offset.</summary>
    Custom,

    /// <summary>Places the first object at the active editor playhead.</summary>
    Current
}

/// <summary>Chooses which target objects are removed before placement.</summary>
public enum PatternOverwriteMode
{
    /// <summary>Leaves all existing target objects in place.</summary>
    NoOverwrite,

    /// <summary>Removes existing objects only inside dense pattern partitions.</summary>
    PartitionedOverwrite,

    /// <summary>Removes existing objects throughout the pattern's time span.</summary>
    CompleteOverwrite
}

/// <summary>Chooses which timing information is retained inside a placed pattern.</summary>
public enum TimingOverwriteMode
{
    /// <summary>Keeps the target beatmap's timing information.</summary>
    OriginalTimingOnly,

    /// <summary>Combines pattern-relative timing with target timing.</summary>
    InPatternRelativeTiming,

    /// <summary>Uses the pattern's absolute timing inside each placed part.</summary>
    InPatternAbsoluteTiming,

    /// <summary>Keeps the pattern's timing information inside each placed part.</summary>
    PatternTimingOnly
}
