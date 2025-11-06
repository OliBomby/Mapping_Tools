namespace Mapping_Tools.Domain.Beatmaps.Types;

/// <summary>
/// Indicates that the type has a duration.
/// </summary>
public interface IHasDuration {
    double Duration { get; }
    double EndTime { get; }
}