using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Events;

namespace Mapping_Tools_Core.BeatmapHelper.Parsing {
    public class OsuStoryboardParser : IParser<Storyboard> {
        public void Parse(Storyboard obj, IReadOnlyCollection<string> lines) {
            // Load up all the stuff
            IEnumerable<string> backgroundAndVideoEventsLines = FileFormatHelper.GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerBackgroundLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerFailLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerPassLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerForegroundLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerOverlayLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            IEnumerable<string> storyboardSoundSamplesLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });

            foreach (string line in backgroundAndVideoEventsLines) {
                obj.BackgroundAndVideoEvents.Add(Event.MakeEvent(line));
            }

            obj.StoryboardLayerBackground.AddRange(Event.ParseEventTree(storyboardLayerBackgroundLines));
            obj.StoryboardLayerFail.AddRange(Event.ParseEventTree(storyboardLayerFailLines));
            obj.StoryboardLayerPass.AddRange(Event.ParseEventTree(storyboardLayerPassLines));
            obj.StoryboardLayerForeground.AddRange(Event.ParseEventTree(storyboardLayerForegroundLines));
            obj.StoryboardLayerOverlay.AddRange(Event.ParseEventTree(storyboardLayerOverlayLines));

            foreach (string line in storyboardSoundSamplesLines) {
                obj.StoryboardSoundSamples.Add(new StoryboardSoundSample(line));
            }
        }

        public Storyboard ParseNew(IReadOnlyCollection<string> lines) {
            var storyboard = new Storyboard();
            Parse(storyboard, lines);

            return storyboard;
        }

        public IEnumerable<string> Serialize(Storyboard obj) {
            yield return "[Events]";
            yield return "//Background and Video events";
            foreach (string s in obj.BackgroundAndVideoEvents.Select(e => e.GetLine())) yield return s;
            yield return "//Break Periods";
            foreach (string s in obj.BreakPeriods.Select(b => b.GetLine())) yield return s;
            yield return "//Storyboard Layer 0 (Background)";
            foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerBackground)) yield return s;
            yield return "//Storyboard Layer 1 (Fail)";
            foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerFail)) yield return s;
            yield return "//Storyboard Layer 2 (Pass)";
            foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerPass)) yield return s;
            yield return "//Storyboard Layer 3 (Foreground)";
            foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerForeground)) yield return s;
            yield return "//Storyboard Layer 4 (Overlay)";
            foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerOverlay)) yield return s;
            yield return "//Storyboard Sound Samples";
            foreach (string s in obj.StoryboardSoundSamples.Select(sbss => sbss.GetLine())) yield return s;
            yield return "";
        }
    }
}