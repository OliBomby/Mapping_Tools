namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// How to merge timing of the pattern and the destination beatmap.
/// </summary>
public enum TimingOverwriteMode {
    /// <summary>
    /// Disregards the timing of the pattern and only keeps the timing of the destination beatmap.
    /// </summary>
    DestinationTimingOnly,
    /// <summary>
    /// Multiplies the BPM of the pattern and the destination beatmap together.
    /// </summary>
    InPatternRelativeTiming,
    /// <summary>
    /// Uses destination beatmap BPM for all redlines in the pattern that have the default BPM and uses the pattern BPM for all non-default BPM redlines in the pattern.
    /// </summary>
    InPatternAbsoluteTiming,
    /// <summary>
    /// Uses only the timing of the pattern during the pattern.
    /// </summary>
    PatternTimingOnly
}