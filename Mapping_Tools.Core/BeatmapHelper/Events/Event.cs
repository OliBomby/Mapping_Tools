namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>Base class for objects represented in an osu! storyboard events section.</summary>
public abstract class Event
{
    /// <summary>Initializes an event with an empty child-command collection.</summary>
    protected Event()
    {
        ChildEvents = [];
    }

    /// <summary>Gets or sets the containing loop or trigger, or <see langword="null" /> for a top-level event.</summary>
    public Event? ParentEvent { get; set; }

    /// <summary>Gets or sets the commands nested directly beneath this event.</summary>
    public List<Event> ChildEvents { get; set; }
}
