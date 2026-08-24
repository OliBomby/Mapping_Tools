namespace Mapping_Tools.Core.Tools.PatternGallery.Models;

/// <summary>Chooses the time reference used when placing a pattern.</summary>
public enum ExportTimeMode
{
    /// <summary>Places the pattern at the time encoded by its first object.</summary>
    Pattern,

    /// <summary>Places the first object at a caller-supplied millisecond offset.</summary>
    Custom,

    /// <summary>Places the first object at the active editor playhead.</summary>
    Current,
}

