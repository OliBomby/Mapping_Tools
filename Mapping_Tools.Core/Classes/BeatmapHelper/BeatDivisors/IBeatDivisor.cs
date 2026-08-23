namespace Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
#nullable disable

/// <summary>
///     Describes a snap interval as a fraction of one beat.
/// </summary>
public interface IBeatDivisor : IEquatable<IBeatDivisor>
{
    /// <summary>
    ///     Converts the divisor to its numeric fraction of a beat.
    /// </summary>
    /// <returns>The interval expressed in beats; for example, a 1/4 divisor returns <c>0.25</c>.</returns>
    double GetValue();
}
