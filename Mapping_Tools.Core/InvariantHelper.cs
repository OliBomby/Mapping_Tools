using System;
using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;

namespace Mapping_Tools.Core;

public static class InvariantHelper {
    /// <summary>
    /// Converts the object to a string with invariant culture.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToInvariant(this object obj) {
        return Convert.ToString(obj, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the double to the rounded Invariant string.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToRoundInvariant(this double obj) {
        return Math.Round(obj).ToInvariant();
    }

    /// <summary>
    /// Converts the float to the rounded Invariant string.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToRoundInvariant(this float obj) {
        return Math.Round(obj).ToInvariant();
    }

    public static string ToIntInvariant(this SampleSet obj) {
        return ((int)obj).ToInvariant();
    }

    public static string ToIntInvariant(this StoryboardLayer obj) {
        return ((int)obj).ToInvariant();
    }
}