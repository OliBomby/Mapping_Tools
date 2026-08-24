using System.Globalization;

namespace Mapping_Tools.Application.Timeline;

/// <summary>Identifies a theme-resolved semantic marker style.</summary>
public enum TimelineMarkerKind
{
    /// <summary>Represents a general event without change semantics.</summary>
    Neutral,

    /// <summary>Represents a newly added map element.</summary>
    Added,

    /// <summary>Represents an existing map element that was modified.</summary>
    Changed,

    /// <summary>Represents a removed map element.</summary>
    Removed,

    /// <summary>Represents a feature-specific highlighted event.</summary>
    Accent,
}

