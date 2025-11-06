namespace Mapping_Tools.Domain.Beatmaps.Types;

/// <summary>
/// Indicates that the type has a duration.
/// </summary>
public interface IDuration : IHasDuration {
    void SetDuration(double duration);

    void SetEndTime(double newEndTime);
}