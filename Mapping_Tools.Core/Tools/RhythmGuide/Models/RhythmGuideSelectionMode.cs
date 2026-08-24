namespace Mapping_Tools.Core.Tools.RhythmGuide.Models;

/// <summary>Determines which expanded timeline events become guide objects.</summary>
public enum RhythmGuideSelectionMode
{
    /// <summary>Includes every expanded timeline event.</summary>
    AllEvents,

    /// <summary>Includes only events that carry hitsounds.</summary>
    HitsoundEvents,

    /// <summary>Separates coincident expanded events into distinct guide objects.</summary>
    AllEventSeparated,

    /// <summary>Creates long-note guide objects where the source rhythm permits them.</summary>
    LongNotes,
}

