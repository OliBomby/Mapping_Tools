namespace Mapping_Tools.Core.Classes.BeatmapHelper.Enums {
#nullable disable

    /// <summary>
    /// Identifies the gameplay shape represented by a parsed hit object.
    /// </summary>
    public enum HitObjectType {
        /// <summary>
        /// A single hit circle.
        /// </summary>
        Circle,
        /// <summary>
        /// A slider with a curve, repeat count, and pixel length.
        /// </summary>
        Slider,
        /// <summary>
        /// A spinner active over a time interval.
        /// </summary>
        Spinner,
        /// <summary>
        /// An osu!mania hold note active over a time interval.
        /// </summary>
        HoldNote
    }
}
