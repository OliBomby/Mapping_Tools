// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Enums {

    /// <summary>
    /// The Slider curve type relating to the osu beatmap.
    /// </summary>
#nullable disable

    public enum PathType {
        /// <summary>
        /// The slider curve using Catmull
        /// </summary>
        Catmull,

        /// <summary>
        /// The slider curve using Bezier
        /// </summary>
        Bezier,

        /// <summary>
        /// The slider curve using Linear
        /// </summary>
        Linear,

        /// <summary>
        /// A degree-annotated B-spline encoded with the lazer <c>B4</c> path token.
        /// </summary>
        PerfectCurve,

        /// <summary>
        /// B-spline curve from lazer
        /// </summary>
        BSpline,
    }
}
