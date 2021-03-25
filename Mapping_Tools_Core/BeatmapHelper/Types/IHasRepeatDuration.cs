namespace Mapping_Tools_Core.BeatmapHelper.Types {
    /// <summary>
    /// Indicates that the type has a duration of a single repeat with possibly multiple repeats.
    /// </summary>
    public interface IHasRepeatDuration {
        double RepeatDuration { get; set; }
    }
}