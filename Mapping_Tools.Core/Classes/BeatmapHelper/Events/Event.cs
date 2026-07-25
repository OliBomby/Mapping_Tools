using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Abstract event type. Represents everything that can be put in the [Events] section.
    /// TODO: When actually doing storyboard stuff some of the types should have child and parent events instead of indents, so we get a tree structure. BTW this would break ITextLine
    /// </summary>
#nullable disable

    public abstract class Event : ITextLine {
        /// <summary>
        /// Initializes an event with an empty child-command collection.
        /// </summary>
        protected Event() {
            ChildEvents = new List<Event>();
        }

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
                case "Sprite":
                    myEvent = new Sprite();
                    break;
                case "Animation":
                    myEvent = new Animation();
                    break;
                case "Sample":
                    myEvent = new StoryboardSoundSample();
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
                default:
                    myEvent = new OtherCommand();
                    break;
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
            LinkedList<Event> parentEvents = new LinkedList<Event>();
            Event lastEvent = null;
            int lastIndents = -1;  // -1 is below the lowest possible indents, so this will always trigger adding null in the parent events
            foreach (var line in lines) {
                var ev = MakeEvent(line);
                int indents = ParseIndents(line);

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
                var parent = parentEvents.Last.Value;
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
        /// <param name="saveWithFloatPrecision">Whether to set SaveWithFloatPrecision to true on the events before serializing.</param>
        /// <returns></returns>
        public static IEnumerable<string> SerializeEventTree(IEnumerable<Event> events, int depth = 0, bool saveWithFloatPrecision = false) {
            foreach (var ev in events) {
                if (saveWithFloatPrecision)
                    ev.SaveWithFloatPrecision = true;
                yield return GetIndents(depth) + ev.GetLine();
                if (ev.ChildEvents.Count > 0) {
                    foreach (var childLine in SerializeEventTree(ev.ChildEvents, depth + 1, saveWithFloatPrecision)) {
                        yield return childLine;
                    }
                }
            }
        }

        /// <summary>
        /// Creates the whitespace prefix used for a storyboard command depth.
        /// </summary>
        /// <param name="count">The number of spaces.</param>
        /// <returns>A string containing exactly <paramref name="count"/> spaces.</returns>
        public static string GetIndents(int count) {
            return new string(' ', count);
        }

        /// <summary>
        /// Counts leading whitespace in a serialized event line.
        /// </summary>
        /// <param name="line">The serialized event line.</param>
        /// <returns>The command's indentation depth.</returns>
        public static int ParseIndents(string line) {
            return line.TakeWhile(char.IsWhiteSpace).Count();
        }

        /// <summary>
        /// Removes all leading whitespace from a serialized event line.
        /// </summary>
        /// <param name="line">The serialized event line.</param>
        /// <returns>The event data beginning at its command token.</returns>
        public static string RemoveIndents(string line) {
            return line.Substring(ParseIndents(line));
        }

        /// <summary>
        /// Serializes the event without parent-tree indentation.
        /// </summary>
        /// <returns>The osu! event line.</returns>
        public abstract string GetLine();

        /// <summary>
        /// Replaces this event's state by parsing an osu! event line.
        /// </summary>
        /// <param name="line">The serialized event line, optionally indented.</param>
        public abstract void SetLine(string line);

        /// <summary>
        /// Gets or sets the containing loop or trigger, or <see langword="null"/>
        /// for a top-level event.
        /// </summary>
        public Event ParentEvent { get; set; }

        /// <summary>
        /// Gets or sets commands nested directly beneath this event.
        /// </summary>
        public List<Event> ChildEvents { get; set; }

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        [JsonIgnore]
        public bool SaveWithFloatPrecision { get; set; }
    }
}
