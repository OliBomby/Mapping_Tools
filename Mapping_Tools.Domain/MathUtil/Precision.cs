// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Mapping_Tools.Domain.MathUtil;

/// <summary>
/// Helper class for operations with double precision numbers.
/// </summary>
public static class Precision {
    /// <summary>
    /// Default error margin for double precision numbers.
    /// </summary>
    public const double DoubleEpsilon = 1e-7;

    /// <summary>
    /// Default error margin for float precision numbers.
    /// </summary>
    public const float FloatEpsilon = 1e-5f;

    /// <summary>
    /// Checks if value1 is bigger than value2 by some margin.
    /// </summary>
    public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
        return value1 - acceptableDifference > value2;
    }

    /// <summary>
    /// Checks if value1 is smaller than value2 by some margin.
    /// </summary>
    public static bool DefinitelySmaller(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
        return value1 < value2 - acceptableDifference;
    }

    /// <summary>
    /// Checks if value1 is almost bigger than value2 with some acceptable difference.
    /// </summary>
    public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
        return value1 > value2 - acceptableDifference;
    }

    /// <summary>
    /// Checks if value1 is almost smaller than value2 with some acceptable difference.
    /// </summary>
    public static bool AlmostSmaller(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
        return value1 - acceptableDifference < value2;
    }

    /// <summary>
    /// Checks if value1 is equal to value2 with some acceptable difference.
    /// </summary>
    public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
        return Math.Abs(value1 - value2) <= acceptableDifference;
    }

    /// <summary>
    /// Checks if value1 is equal to value2 with some acceptable difference.
    /// </summary>
    public static bool AlmostEquals(Vector2 value1, Vector2 value2, double acceptableDifference = DoubleEpsilon) {
        return AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);
    }
}