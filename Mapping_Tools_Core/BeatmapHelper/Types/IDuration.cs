namespace Mapping_Tools_Core.BeatmapHelper.Types {
    /// <summary>
    /// Indicates that the type has a duration.
    /// </summary>
    public interface IDuration : IHasDuration {
        void SetDuration(double duration);

        void SetEndTime(double newEndTime);
    }
}