using System;
using Mapping_Tools_Core.BeatmapHelper.Events;
using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public class OsuStoryboardDecoder : IDecoder<Storyboard> {
        public void Decode(Storyboard obj, string code) {
            var lines = code.Split(Environment.NewLine);

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

        public Storyboard DecodeNew(string code) {
            var storyboard = new Storyboard();
            Decode(storyboard, code);

            return storyboard;
        }
    }
}