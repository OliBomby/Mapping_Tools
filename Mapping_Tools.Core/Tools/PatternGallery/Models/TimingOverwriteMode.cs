namespace Mapping_Tools.Core.Tools.PatternGallery.Models;

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
    PatternTimingOnly,
}
