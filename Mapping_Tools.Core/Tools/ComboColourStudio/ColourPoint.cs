using System;
using System.Collections.Generic;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <inheritdoc cref="IColourPoint"/>
public class ColourPoint : IColourPoint, IEquatable<ColourPoint> {
    private readonly List<int> _colourSequence;

    /// <inheritdoc/>
    public double Time { get; }

    /// <inheritdoc/>
    public ColourPointMode Mode { get; }

    /// <inheritdoc/>
    public IReadOnlyList<int> ColourSequence => _colourSequence;

    /// <summary>
    /// Creates a new colour point.
    /// </summary>
    /// <param name="time">The absolute time in milliseconds.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="colourSequence">The sequence of colour indices.</param>
    public ColourPoint(double time, ColourPointMode mode, IEnumerable<int> colourSequence) {
        Time = time;
        Mode = mode;
        _colourSequence = new List<int>(colourSequence);
    }

    /// <summary>
    /// Creates a clone of this colour point.
    /// </summary>
    /// <returns>The clone.</returns>
    public ColourPoint Clone() {
        return new ColourPoint(Time, Mode, ColourSequence);
    }

    /// <inheritdoc/>
    public bool Equals(ColourPoint other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(_colourSequence, other._colourSequence) && Time.Equals(other.Time) && Mode == other.Mode;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ColourPoint) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() {
        unchecked {
            var hashCode = (_colourSequence != null ? _colourSequence.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Time.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) Mode;
            return hashCode;
        }
    }
}