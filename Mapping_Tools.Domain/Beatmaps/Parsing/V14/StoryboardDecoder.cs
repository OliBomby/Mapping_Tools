using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Types;
using static Mapping_Tools.Domain.Beatmaps.Parsing.FileFormatHelper;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class StoryboardDecoder : IDecoder<Storyboard> {
    private IEnumerable<IEnumerable<string>> EnumerateSections(IEnumerable<string> lines) {
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
    private IEnumerable<string> EnumerateSection(IEnumerator<string> em) {
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

    public Storyboard Decode(string code) {
        var storyboard = new Storyboard();
        
        var lines = code.Split('\n').Select(l => l.Trim('\r')).ToList();

        IEnumerable<string> eventsLines = GetCategoryLines(lines, "[Events]");

        foreach (var section in EnumerateSections(eventsLines)) {
            var events = Event.ParseEventTree(section);
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
                                // Unexpected layer. Event gets placed nowhere
                                break;
                        }
                        break;
                    default:
                        // Unexpected command. Can be warned but its no big deal.
                        break;
                }
            }
        }

        return storyboard;
    }
}