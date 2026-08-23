namespace Mapping_Tools.Core.BeatmapHelper.Events;
#nullable disable

/// <summary>
///     Base class for indented storyboard commands that begin at a specific time.
/// </summary>
public abstract class Command : Event, IHasStartTime
{
    /// <summary>
    ///     Gets or sets the indentation depth observed in the source line.
    /// </summary>
    public int Indents { get; set; }

    /// <summary>
    ///     Gets or sets the serialized storyboard command token.
    /// </summary>
    public virtual EventType EventType { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public double StartTime { get; set; }
}
