namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Indicates that a type has an end time. Used by Property Transformer on Events
/// </summary>
public interface IHasEndTime
{
    /// <summary>
    ///     Gets or sets the absolute end time in milliseconds.
    /// </summary>
    double EndTime { get; set; }
}
