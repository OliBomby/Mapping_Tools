using System;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// A mapping pattern of Pattern Gallery.
/// </summary>
public interface IOsuPattern : IEquatable<IOsuPattern> {
    /// <summary>
    /// The name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// The time at which the pattern got created.
    /// </summary>
    DateTime CreationTime { get; set; }

    /// <summary>
    /// The last time the pattern was used.
    /// </summary>
    DateTime LastUsedTime { get; set; }

    /// <summary>
    /// How many times the pattern has been used.
    /// </summary>
    int UseCount { get; set; }

    /// <summary>
    /// The filename of the file storing the pattern data.
    /// </summary>
    string Filename { get; }

    /// <summary>
    /// The number of hit objects in the pattern.
    /// </summary>
    int ObjectCount { get; }

    /// <summary>
    /// The native duration of the pattern.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// The number of beats in the duration of the pattern.
    /// </summary>
    double BeatLength { get; }
}