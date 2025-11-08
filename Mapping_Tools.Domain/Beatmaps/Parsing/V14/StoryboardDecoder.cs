using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;
using Mapping_Tools.Domain.Beatmaps.Types;
using static Mapping_Tools.Domain.Beatmaps.Parsing.FileFormatHelper;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class StoryboardDecoder(IDecoder<Event> eventDecoder) : IDecoder<Storyboard> {
    public StoryboardDecoder() : this(new EventDecoder()) { }

    public Storyboard Decode(string code) {
        var storyboard = new Storyboard();
        
        var lines = code.Split('\n').Select(l => l.Trim('\r')).ToList();

        IEnumerable<string> eventsLines = GetCategoryLines(lines, "[Events]");

        foreach (var section in EnumerateSections(eventsLines)) {
            var events = ParseEventTree(section);

            foreach (var ev in events) {
                switch (ev) {
                    case Background:
                    case Video:
                        storyboard.BackgroundAndVideoEvents.Add(ev);
                        break;
                    case Break b:
                        storyboard.BreakPeriods.Add(b);
                        break;
                    case StoryboardSoundSample sbss:
                        storyboard.StoryboardSoundSamples.Add(sbss);
                        break;
                    case BackgroundColourTransformation bct:
                        storyboard.BackgroundColourTransformations.Add(bct);
                        break;
                    case IHasStoryboardLayer l:
                        switch (l.Layer) {
                            case StoryboardLayer.Background:
                                storyboard.StoryboardLayerBackground.Add(ev);
                                break;
                            case StoryboardLayer.Fail:
                                storyboard.StoryboardLayerFail.Add(ev);
                                break;
                            case StoryboardLayer.Pass:
                                storyboard.StoryboardLayerPass.Add(ev);
                                break;
                            case StoryboardLayer.Foreground:
                                storyboard.StoryboardLayerForeground.Add(ev);
                                break;
                            case StoryboardLayer.Overlay:
                                storyboard.StoryboardLayerOverlay.Add(ev);
                                break;
                            default:
                                throw new BeatmapParsingException();
                        }
                        break;
                    default:
                        throw new BeatmapParsingException();
                }
            }
        }

        return storyboard;
    }

    private static IEnumerable<IEnumerable<string>> EnumerateSections(IEnumerable<string> lines) {
        var em = lines.GetEnumerator();
        while (em.MoveNext()) {
            yield return EnumerateSection(em);
            if (em.Current.StartsWith('['))
                yield break;
        }
    }

    /// <summary>
    /// Enumerates the enumerator until it reaches a // or [.
    /// </summary>
    private static IEnumerable<string> EnumerateSection(IEnumerator<string> em) {
        var line = em.Current;
        if (!string.IsNullOrWhiteSpace(line)) {
            if (line.StartsWith("//") || line.StartsWith('[')) {
                yield break;
            }
            yield return line;
        }
        while (em.MoveNext()) {
            line = em.Current;
            if (!string.IsNullOrWhiteSpace(line)) {
                if (line.StartsWith("//") || line.StartsWith('[')) {
                    yield break;
                }
                yield return line;
            }
        }
    }

    /// <summary>
    /// Takes a collection of lines and parses them as <see cref="Event"/> in a tree structure.
    /// Only the top level events get returned.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private IEnumerable<Event> ParseEventTree(IEnumerable<string> lines) {
        LinkedList<Event> parentEvents = new();
        Event? lastEvent = null;
        int lastIndents = -1;  // -1 is below the lowest possible indents, so this will always trigger adding null in the parent events
        foreach (var line in lines) {
            int indents = ParseIndents(line);
            var ev = eventDecoder.Decode(line[indents..]);

            if (indents > lastIndents && lastEvent is not null) {
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

    private static int ParseIndents(string line) {
        return line.TakeWhile(c => char.IsWhiteSpace(c) || c == '_').Count();
    }
}