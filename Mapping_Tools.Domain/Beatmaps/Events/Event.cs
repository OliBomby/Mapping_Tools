namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Abstract event type. Represents everything that can be put in the [Events] section.
/// TODO: When actually doing storyboard stuff some of the types should have child and parent events instead of indents, so we get a tree structure.
/// </summary>
public abstract class Event {

    public Event? ParentEvent { get; set; }

    public List<Event> ChildEvents { get; } = [];

    /// <summary>
    /// Factory method for making an <see cref="Event"/> from a serialized line of .osu code.
    /// Automatically recognizes the type of the event from the string and makes the appropriate object.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Event MakeEvent(string line) {
        string[] values = line.Split(',');
        string eventType = values[0].Trim();

        Event myEvent;
        switch (eventType) {
            case "0":
            case "Background":
                myEvent = new Background();
                break;
            case "1":
            case "Video":
                myEvent = new Video();
                break;
            case "2":
            case "Break":
                myEvent = new Break();
                break;
            case "3":
            case "Colour":
                myEvent = new BackgroundColourTransformation();
                break;
            case "4":
            case "Sprite":
                myEvent = new Sprite();
                break;
            case "5":
            case "Sample":
                myEvent = new StoryboardSoundSample();
                break;
            case "6":
            case "Animation":
                myEvent = new Animation();
                break;
            case "P":
                myEvent = new ParameterCommand();
                break;
            case "L":
                myEvent = new StandardLoop();
                break;
            case "T":
                myEvent = new TriggerLoop();
                break;
            case "F":
            case "M":
            case "MX":
            case "MY":
            case "S":
            case "V":
            case "R":
            case "C":
                myEvent = new OtherCommand();
                break;
            default:
                return null;
        }

        myEvent.SetLine(line);

        return myEvent;
    }

    /// <summary>
    /// Takes a collection of lines and parses them as <see cref="Event"/> in a tree structure.
    /// Only the top level events get returned.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static IEnumerable<Event> ParseEventTree(IEnumerable<string> lines) {
        LinkedList<Event> parentEvents = new();
        Event lastEvent = null;
        int lastIndents = -1;  // -1 is below the lowest possible indents, so this will always trigger adding null in the parent events
        foreach (var line in lines) {
            int indents = ParseIndents(line);
            var ev = MakeEvent(line[indents..]);

            // Transforms with an illegal type are ignored
            if (ev == null)
                continue;

            // Add the indent count to any command type events
            if (ev is Command c) c.Indents = indents;

            if (indents > lastIndents) {
                // Go deeper in the tree
                parentEvents.AddLast(lastEvent);
            } else if (indents < lastIndents) {
                // Go back in the tree until the last parent has exactly one less indent
                // Because each parent layer has exactly one more indent we know how many layers to go back
                for (int i = 0; i < lastIndents - indents; i++) {
                    parentEvents.RemoveLast();
                }
            }

            // Add this event to the tree or return it if it's at the top level
            var parent = parentEvents.Last?.Value;
            if (parent == null) {
                yield return ev;
            } else {
                parent.ChildEvents.Add(ev);
                ev.ParentEvent = parent;
            }

            lastEvent = ev;
            lastIndents = indents;
        }
    }

    /// <summary>
    /// Converts an events tree into a string representation.
    /// </summary>
    /// <param name="events">Collection of top level events.</param>
    /// <param name="depth">Indent count for the top level of events.</param>
    /// <returns></returns>
    public static IEnumerable<string> SerializeEventTree(IEnumerable<Event> events, int depth = 0) {
        foreach (var ev in events) {
            yield return GetIndents(depth) + ev.GetLine();
            if (ev.ChildEvents.Count > 0) {
                foreach (var childLine in SerializeEventTree(ev.ChildEvents, depth + 1)) {
                    yield return childLine;
                }
            }
        }
    }

    public static string GetIndents(int count) {
        return new string(' ', count);
    }

    public static int ParseIndents(string line) {
        return line.TakeWhile(c => char.IsWhiteSpace(c) || c == '_').Count();
    }

    public static string RemoveIndents(string line) {
        return line[ParseIndents(line)..];
    }

    public abstract string GetLine();

    public abstract void SetLine(string line);
}