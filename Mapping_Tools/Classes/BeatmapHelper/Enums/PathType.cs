// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Mapping_Tools.Classes.BeatmapHelper.Enums {

    /// <summary>
    /// The Slider curve type relating to the osu beatmap.
    /// </summary>
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
        /// 
        /// </summary>
        PerfectCurve
    }
}