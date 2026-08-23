// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Mapping_Tools.Core.Classes.MathUtil;

/// <summary>
///     Centralizes tolerance-aware comparisons used by geometry algorithms.
/// </summary>
public static class Precision
{
    /// <summary>
    ///     The default absolute tolerance used for double-precision comparisons.
    /// </summary>
    public const double DoubleEpsilon = 1e-7;

    /// <summary>
    ///     Tests whether the first value exceeds the second by more than the accepted tolerance.
    /// </summary>
    /// <param name="value1">The candidate larger value.</param>
    /// <param name="value2">The comparison value.</param>
    /// <param name="acceptableDifference">The interval treated as numerically indistinguishable.</param>
    /// <returns>
    ///     <see langword="true" /> when <paramref name="value1" /> is outside the tolerance above
    ///     <paramref name="value2" />.
    /// </returns>
    public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon)
    {
        return value1 - acceptableDifference > value2;
    }

    /// <summary>
    ///     Tests whether the first value is greater than, or within tolerance below, the second.
    /// </summary>
    /// <param name="value1">The candidate larger value.</param>
    /// <param name="value2">The comparison value.</param>
    /// <param name="acceptableDifference">The amount by which the first value may fall short.</param>
    /// <returns><see langword="true" /> when <paramref name="value1" /> is not meaningfully smaller.</returns>
    public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon)
    {
        return value1 > value2 - acceptableDifference;
    }

    /// <summary>
    ///     Compares two scalar values using an inclusive absolute tolerance.
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="acceptableDifference">The maximum accepted absolute difference.</param>
    /// <returns><see langword="true" /> when the values differ by no more than the tolerance.</returns>
    public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DoubleEpsilon)
    {
        return Math.Abs(value1 - value2) <= acceptableDifference;
    }

    /// <summary>
    ///     Compares both components of two vectors using an inclusive absolute tolerance.
    /// </summary>
    /// <param name="value1">The first vector.</param>
    /// <param name="value2">The second vector.</param>
    /// <param name="acceptableDifference">The maximum accepted difference per component.</param>
    /// <returns><see langword="true" /> when both corresponding components are almost equal.</returns>
    public static bool AlmostEquals(Vector2 value1, Vector2 value2, double acceptableDifference = DoubleEpsilon)
    {
        return AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);
    }
}
