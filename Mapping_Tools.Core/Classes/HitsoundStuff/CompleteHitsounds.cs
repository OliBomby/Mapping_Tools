namespace Mapping_Tools.Core.Classes.HitsoundStuff;

/// <summary>
///     Bundles resolved playback events with the custom-index sample requirements needed to render them.
/// </summary>
public class CompleteHitsounds
{
    /// <summary>
    ///     Custom sample-index assignments required by the events.
    /// </summary>
    public List<CustomIndex> CustomIndices;

    /// <summary>
    ///     Resolved hitsound events in playback order.
    /// </summary>
    public List<HitsoundEvent> Hitsounds;

    /// <summary>
    ///     Creates a complete result from events and index assignments.
    /// </summary>
    /// <param name="hitsounds">Resolved playback events.</param>
    /// <param name="customIndices">Required custom-index assignments.</param>
    public CompleteHitsounds(List<HitsoundEvent> hitsounds, List<CustomIndex> customIndices)
    {
        Hitsounds = hitsounds;
        CustomIndices = customIndices;
    }

    /// <summary>
    ///     Creates a result containing events but no custom-index assignments.
    /// </summary>
    /// <param name="hitsounds">Resolved playback events.</param>
    public CompleteHitsounds(List<HitsoundEvent> hitsounds)
    {
        Hitsounds = hitsounds;
        CustomIndices = new List<CustomIndex>();
    }

    /// <summary>
    ///     Creates a result containing custom-index assignments but no playback events.
    /// </summary>
    /// <param name="customIndices">Required custom-index assignments.</param>
    public CompleteHitsounds(List<CustomIndex> customIndices)
    {
        Hitsounds = new List<HitsoundEvent>();
        CustomIndices = customIndices;
    }

    /// <summary>
    ///     Creates an empty hitsound result.
    /// </summary>
    public CompleteHitsounds()
    {
        Hitsounds = new List<HitsoundEvent>();
        CustomIndices = new List<CustomIndex>();
    }
}
