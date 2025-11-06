namespace Mapping_Tools.Domain.Beatmaps.Types;

/// <summary>
/// Indicates that a type has a start time.
/// </summary>
public interface IHasStartTime {
    double StartTime { get; set; }
}