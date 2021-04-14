namespace Mapping_Tools_Core.BeatmapHelper.Types {
    public interface IHasRepeats : IHasDuration {
        /// <summary>
        /// The amount of times the object repeats.
        /// </summary>
        int RepeatCount { get; }

        /// <summary>
        /// The amount of times the length of the object spans.
        /// </summary>
        int SpanCount => RepeatCount + 1;

        /// <summary>
        /// The duration of one span.
        /// </summary>
        double SpanDuration => Duration / SpanCount;
    }
}