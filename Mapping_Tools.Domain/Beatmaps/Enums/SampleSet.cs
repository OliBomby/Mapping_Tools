namespace Mapping_Tools.Domain.Beatmaps.Enums;

/// <summary>
/// The types of samples used for inherited timing points and hitobjects themselves.
/// </summary>
public enum SampleSet {
    /// <summary>
    /// None sampleset. Inherits the actual sample set from something else if possible.
    /// Displays as "Auto" in the osu! client.
    /// </summary>
    None = 0,
    /// <summary>
    /// The sampleset of Normal.
    /// </summary>
    Normal = 1,
    /// <summary>
    /// The sampleset of Soft.
    /// </summary>
    Soft = 2,
    /// <summary>
    /// The sampleset of Drum.
    /// </summary>
    Drum = 3,
}