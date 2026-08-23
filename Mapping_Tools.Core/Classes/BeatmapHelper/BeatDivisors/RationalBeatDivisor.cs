using Newtonsoft.Json;

namespace Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
#nullable disable

/// <summary>
///     Stores a snap interval as an unreduced numerator/denominator fraction of one beat.
/// </summary>
public class RationalBeatDivisor : IBeatDivisor
{
    /// <summary>
    ///     The number below the line in a vulgar fraction; a divisor.
    /// </summary>
    public readonly int Denominator;

    /// <summary>
    ///     The number above the line in a vulgar fraction showing how many of the parts indicated by the denominator are
    ///     taken, for example, 2 in 2/3.
    /// </summary>
    public readonly int Numerator;

    /// <summary>
    ///     Creates the reciprocal divisor <c>1/<paramref name="denominator" /></c>.
    /// </summary>
    /// <param name="denominator">The number of equal subdivisions per beat.</param>
    public RationalBeatDivisor(int denominator)
    {
        Numerator = 1;
        Denominator = denominator;
    }

    /// <summary>
    ///     Creates a divisor from an explicit, unreduced fraction.
    /// </summary>
    /// <param name="numerator">The number of subdivisions spanned by the interval.</param>
    /// <param name="denominator">The number of equal subdivisions per beat.</param>
    [JsonConstructor]
    public RationalBeatDivisor(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    public double GetValue()
    {
        return (double)Numerator / Denominator;
    }

    /// <summary>
    ///     Compares this divisor with another divisor of the same representation.
    /// </summary>
    /// <param name="other">The divisor to compare.</param>
    /// <returns><see langword="true" /> only for a rational divisor with identical components.</returns>
    public bool Equals(IBeatDivisor other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other is RationalBeatDivisor otherRational) return Equals(otherRational);
        return false;
    }

    /// <summary>
    ///     Converts a subdivision count to its reciprocal beat divisor.
    /// </summary>
    /// <param name="denominator">The number of equal subdivisions per beat.</param>
    /// <returns>The divisor <c>1/<paramref name="denominator" /></c>.</returns>
    public static implicit operator RationalBeatDivisor(int denominator) => new(denominator);

    /// <summary>
    ///     Compares the stored numerator and denominator without reducing either fraction.
    /// </summary>
    /// <param name="other">The rational divisor to compare.</param>
    /// <returns><see langword="true" /> when both stored fraction components match.</returns>
    protected bool Equals(RationalBeatDivisor other)
    {
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    /// <summary>
    ///     Determines whether an object is a rational divisor with identical components.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when the runtime types, numerator, and denominator match.</returns>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((RationalBeatDivisor)obj);
    }

    /// <summary>
    ///     Combines the unreduced numerator and denominator into a hash code.
    /// </summary>
    /// <returns>A hash code consistent with component-wise equality.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return Numerator * 397 ^ Denominator;
        }
    }

    /// <summary>
    ///     Returns the fine snap divisors offered by the default beat-divisor configuration.
    /// </summary>
    /// <returns>Divisors for 1/16 and 1/12 of a beat.</returns>
    public static IBeatDivisor[] GetDefaultBeatDivisors()
    {
        return new IBeatDivisor[] { new RationalBeatDivisor(16), new RationalBeatDivisor(12) };
    }

    /// <summary>
    ///     Formats the stored fraction without reducing it.
    /// </summary>
    /// <returns>A <c>numerator/denominator</c> string.</returns>
    public override string ToString()
    {
        return $"{Numerator}/{Denominator}";
    }
}
