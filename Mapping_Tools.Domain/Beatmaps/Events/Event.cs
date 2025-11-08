namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Abstract event type. Represents everything that can be put in the [Events] section.
/// </summary>
public abstract class Event {
    public virtual string EventType { get; set; } = null!;

    public Event? ParentEvent { get; set; }

    public List<Event> ChildEvents { get; } = [];
}