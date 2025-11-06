using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.Types;
using static Mapping_Tools.Core.BeatmapHelper.IO.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding;

public class OsuStoryboardDecoder : IDecoder<Storyboard> {
    public void Decode(Storyboard obj, string code) {
        var lines = code.Split('\n').Select(l => l.Trim('\r')).ToList();

        IEnumerable<string> eventsLines = GetCategoryLines(lines, "[Events]");

        foreach (var section in EnumerateSections(eventsLines)) {
            var events = Event.ParseEventTree(section);
            foreach (var ev in events) {
                switch (ev) {
                    case Background:
                    case Video:
                        obj.BackgroundAndVideoEvents.Add(ev);
                        break;
                    case Break b:
                        obj.BreakPeriods.Add(b);
                        break;
                    case StoryboardSoundSample sbss:
                        obj.StoryboardSoundSamples.Add(sbss);
                        break;
                    case BackgroundColourTransformation bct:
                        obj.BackgroundColourTransformations.Add(bct);
                        break;
                    case IHasStoryboardLayer l:
                        switch (l.Layer) {
                            case StoryboardLayer.Background:
                                obj.StoryboardLayerBackground.Add(ev);
                                break;
                            case StoryboardLayer.Fail:
                                obj.StoryboardLayerFail.Add(ev);
                                break;
                            case StoryboardLayer.Pass:
                                obj.StoryboardLayerPass.Add(ev);
                                break;
                            case StoryboardLayer.Foreground:
                                obj.StoryboardLayerForeground.Add(ev);
                                break;
                            case StoryboardLayer.Overlay:
                                obj.StoryboardLayerOverlay.Add(ev);
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
    }

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
        Decode(storyboard, code);

        return storyboard;
    }
}