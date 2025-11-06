namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Where in time to export a pattern.
/// </summary>
public enum ExportTimeMode {
    /// <summary>
    /// Exports the pattern at the native offset of the pattern.
    /// </summary>
    Pattern,
    /// <summary>
    /// Exports the pattern at a custom offset.
    /// </summary>
    Custom,
    /// <summary>
    /// Exports the pattern at the current time.
    /// </summary>
    Current
}