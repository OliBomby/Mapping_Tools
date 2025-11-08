using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class StoryboardEncoder(IEncoder<Event> eventEncoder) : IEncoder<Storyboard> {
    public StoryboardEncoder() : this(new EventEncoder()) { }

    public string Encode(Storyboard obj) {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("[Events]");
        builder.AppendLine("//Background and Video events");
        foreach (string s in obj.BackgroundAndVideoEvents.Select(eventEncoder.Encode)) builder.AppendLine(s);
        builder.AppendLine("//Break Periods");
        foreach (string s in obj.BreakPeriods.Select(eventEncoder.Encode)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 0 (Background)");
        foreach (string s in SerializeEventTree(obj.StoryboardLayerBackground)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 1 (Fail)");
        foreach (string s in SerializeEventTree(obj.StoryboardLayerFail)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 2 (Pass)");
        foreach (string s in SerializeEventTree(obj.StoryboardLayerPass)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 3 (Foreground)");
        foreach (string s in SerializeEventTree(obj.StoryboardLayerForeground)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 4 (Overlay)");
        foreach (string s in SerializeEventTree(obj.StoryboardLayerOverlay)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Sound Samples");
        foreach (string s in obj.StoryboardSoundSamples.Select(eventEncoder.Encode)) builder.AppendLine(s);
        if (obj.BackgroundColourTransformations.Count > 0) {
            builder.AppendLine("//Background Colour Transformations");
            foreach (string s in obj.BackgroundColourTransformations.Select(eventEncoder.Encode)) builder.AppendLine(s);
        }

        builder.AppendLine();
        return builder.ToString();
    }

    /// <summary>
    /// Converts an events tree into a string representation.
    /// </summary>
    /// <param name="events">Collection of top level events.</param>
    /// <param name="depth">Indent count for the top level of events.</param>
    /// <returns></returns>
    private IEnumerable<string> SerializeEventTree(IEnumerable<Event> events, int depth = 0) {
        foreach (var ev in events) {
            yield return GetIndents(depth) + eventEncoder.Encode(ev);

            if (ev.ChildEvents.Count > 0) {
                foreach (var childLine in SerializeEventTree(ev.ChildEvents, depth + 1)) {
                    yield return childLine;
                }
            }
        }
    }

    private static string GetIndents(int count) {
        return new string(' ', count);
    }
}