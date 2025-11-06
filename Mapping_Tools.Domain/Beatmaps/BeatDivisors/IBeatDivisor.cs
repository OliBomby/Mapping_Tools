namespace Mapping_Tools.Domain.Beatmaps.BeatDivisors;

/// <summary>
/// A beat divisor for osu!
/// </summary>
public interface IBeatDivisor : IEquatable<IBeatDivisor> {
    /// <summary>
    /// Gets the beat length multiplier for the beat divisor.
    /// For example: A value of 0.25 is a 1/4 beat divisor.
    /// </summary>
    /// <returns>The beat length multiplier for the beat divisor.</returns>
    double GetValue();
}