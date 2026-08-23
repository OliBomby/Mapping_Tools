namespace Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
#nullable disable

/// <summary>
///     Stores a beat fraction that cannot or should not be represented as a numerator and denominator.
/// </summary>
public class IrrationalBeatDivisor : IBeatDivisor
{
    /// <summary>
    ///     The interval expressed as a fraction of one beat.
    /// </summary>
    public readonly double Value;

    /// <summary>
    ///     Creates a divisor from an arbitrary numeric beat fraction.
    /// </summary>
    /// <param name="value">The interval as a fraction of one beat.</param>
    public IrrationalBeatDivisor(double value)
    {
        Value = value;
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>
    ///     Compares this divisor with another divisor of the same representation.
    /// </summary>
    /// <param name="other">The divisor to compare.</param>
    /// <returns><see langword="true" /> only for an irrational divisor with the same stored value.</returns>
    public bool Equals(IBeatDivisor other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other is IrrationalBeatDivisor otherIrrational) return Equals(otherIrrational);
        return false;
    }

    /// <summary>
    ///     Wraps an arbitrary beat fraction in an irrational divisor.
    /// </summary>
    /// <param name="value">The interval as a fraction of one beat.</param>
    /// <returns>A divisor containing <paramref name="value" /> unchanged.</returns>
    public static implicit operator IrrationalBeatDivisor(double value) => new(value);

    /// <summary>
    ///     Compares two irrational divisors by their exact stored <see cref="double" /> value.
    /// </summary>
    /// <param name="other">The divisor to compare.</param>
    /// <returns><see langword="true" /> when both values compare equal.</returns>
    protected bool Equals(IrrationalBeatDivisor other)
    {
        return Value.Equals(other.Value);
    }

    /// <summary>
    ///     Determines whether an object is an irrational divisor with the same stored value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when the runtime types and stored values match.</returns>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((IrrationalBeatDivisor)obj);
    }

    /// <summary>
    ///     Returns the hash code of the stored beat fraction.
    /// </summary>
    /// <returns>A hash code consistent with exact-value equality.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    ///     Formats the beat fraction using invariant culture.
    /// </summary>
    /// <returns>The numeric fraction without locale-dependent decimal separators.</returns>
    public override string ToString()
    {
        return GetValue().ToInvariant();
    }
}
