
using System.Text.Json.Serialization;

namespace Mapping_Tools.Domain.Beatmaps.BeatDivisors;

[method: JsonConstructor]
public class RationalBeatDivisor(int numerator, int denominator) : IBeatDivisor {
    /// <summary>
    /// The number above the line in a vulgar fraction showing how many of the parts indicated by the denominator are taken, for example, 2 in 2/3.
    /// </summary>
    public int Numerator { get; } = numerator;

    /// <summary>
    /// The number below the line in a vulgar fraction; a divisor.
    /// </summary>
    public int Denominator { get; } = denominator;

    public RationalBeatDivisor(int denominator) : this(1, denominator) {
    }

    public static implicit operator RationalBeatDivisor(int denominator) {
        return new RationalBeatDivisor(denominator);
    }

    public double GetValue() {
        return (double) Numerator / Denominator;
    }

    protected bool Equals(RationalBeatDivisor? other) {
        return other is not null && Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public bool Equals(IBeatDivisor? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other is RationalBeatDivisor otherRational) return Equals(otherRational);
        return false;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((RationalBeatDivisor) obj);
    }

    public override int GetHashCode() {
        unchecked {
            return Numerator * 397 ^ Denominator;
        }
    }

    public static IBeatDivisor[] GetDefaultBeatDivisors() {
        return [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
    }
}