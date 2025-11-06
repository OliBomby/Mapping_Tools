using System;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Must store the objects, the greenlines, the timing, the global SV, the tickrate, the difficulty settings,
/// the hitsounds, absolute times and positions, combocolour index, combo numbers, stack leniency, gamemode.
/// Also store additional metadata such as the name, the date it was saved, use count, the map title, artist, diffname, and mapper.
/// </summary>
public class OsuPattern : IOsuPattern {
    #region Fields

    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public DateTime CreationTime { get; set; }
    /// <inheritdoc/>
    public DateTime LastUsedTime { get; set; }
    /// <inheritdoc/>
    public int UseCount { get; set; }
    /// <inheritdoc/>
    public string Filename { get; }
    /// <inheritdoc/>
    public int ObjectCount { get; }
    /// <inheritdoc/>
    public TimeSpan Duration { get; }
    /// <inheritdoc/>
    public double BeatLength { get; }

    #endregion

    /// <summary>
    /// Creates a new osu! mapping pattern.
    /// </summary>
    /// <param name="filename">The filename of the file storing the pattern data.</param>
    /// <param name="objectCount">The number of hit objects in the pattern.</param>
    /// <param name="duration">The native duration of the pattern.</param>
    /// <param name="beatLength">The number of beats in the duration of the pattern.</param>
    public OsuPattern(string filename, int objectCount, TimeSpan duration, double beatLength) {
        Filename = filename;
        ObjectCount = objectCount;
        Duration = duration;
        BeatLength = beatLength;
    }

    /// <inheritdoc/>
    public bool Equals(IOsuPattern other) {
        return other != null && Filename == other.Filename;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((IOsuPattern) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() {
        return (Filename != null ? Filename.GetHashCode() : 0);
    }
}