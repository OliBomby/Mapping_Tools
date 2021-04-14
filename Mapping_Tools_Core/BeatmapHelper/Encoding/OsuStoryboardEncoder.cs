using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapping_Tools_Core.BeatmapHelper.Events;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public class OsuStoryboardEncoder : IEnumeratingEncoder<IStoryboard> {
        public IEnumerable<string> EncodeEnumerable(IStoryboard obj) {
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

        public string Encode(IStoryboard obj) {
            return string.Join(Environment.NewLine, EncodeEnumerable(obj));
        }
    }
}