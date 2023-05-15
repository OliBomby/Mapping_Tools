﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Mapping_Tools.Classes.MathUtil {
    public static class Precision {
        public const double DoubleEpsilon = 1e-7;

        public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
            return value1 - acceptableDifference > value2;
        }

        public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
            return value1 > value2 - acceptableDifference;
        }

        public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DoubleEpsilon) {
            return Math.Abs(value1 - value2) <= acceptableDifference;
        }

        public static bool AlmostEquals(Vector2 value1, Vector2 value2, double acceptableDifference = DoubleEpsilon) {
            return AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);
        }
    }
}