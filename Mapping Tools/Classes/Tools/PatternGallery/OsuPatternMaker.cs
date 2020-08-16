using System;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternMaker {
        public OsuPattern FromSelectedWithSave(Beatmap beatmap, string name, OsuPatternFileHandler fileHandler) {
            var osuPattern = FromSelected(beatmap, name, out var patternBeatmap);

            // Could possibly be saved async
            // TODO: Do something so it saves with float precision coordinates
            Editor.SaveFile(fileHandler.GetPatternPath(osuPattern.FileName), patternBeatmap.GetLines());

            return osuPattern;
        }

        public OsuPattern FromSelected(Beatmap beatmap, string name, out Beatmap patternBeatmap) {
            // Copy it so the changes dont affect the given beatmap object
            patternBeatmap = beatmap.DeepCopy();

            // Remove the storyboarding
            patternBeatmap.StoryboardLayerFail.Clear();
            patternBeatmap.StoryboardLayerPass.Clear();
            patternBeatmap.StoryboardLayerBackground.Clear();
            patternBeatmap.StoryboardLayerForeground.Clear();
            patternBeatmap.StoryboardLayerOverlay.Clear();

            // Keep the selected subset of hit objects
            patternBeatmap.HitObjects = patternBeatmap.HitObjects.Where(h => h.IsSelected).ToList();

            var startTime = patternBeatmap.GetHitObjectStartTime() - 5;
            var endTime = patternBeatmap.GetHitObjectEndTime() + 5;

            // Keep the timing points in the range of the hitobjects
            patternBeatmap.BeatmapTiming.TimingPoints = patternBeatmap.BeatmapTiming.TimingPoints
                .Where(tp => tp.Offset >= startTime && tp.Offset <= endTime).ToList();

            // Add some earlier timing points if necessary
            var firstUnInheritedTimingPoint = patternBeatmap.HitObjects.First().UnInheritedTimingPoint;
            var firstNormalTimingPoint = patternBeatmap.HitObjects.First().TimingPoint;

            if (!patternBeatmap.BeatmapTiming.TimingPoints.Contains(firstUnInheritedTimingPoint)) {
                patternBeatmap.BeatmapTiming.TimingPoints.Add(firstUnInheritedTimingPoint);
            }
            if (!patternBeatmap.BeatmapTiming.TimingPoints.Contains(firstNormalTimingPoint)) {
                patternBeatmap.BeatmapTiming.TimingPoints.Add(firstNormalTimingPoint);
            }
            patternBeatmap.BeatmapTiming.Sort();

            // Generate a file name and save the pattern
            var now = DateTime.UtcNow;
            var fileName = GeneratePatternFileName(patternBeatmap.GetFileName(), now);

            return new OsuPattern {
                Name = name,
                SaveDateTime = now,
                FileName = fileName
            };
        }

        public string GeneratePatternFileName(string name, DateTime time) {
            var filename = time.ToString("yyyy-MM-dd HH-mm-ss") + "___" + name;

            if (!filename.EndsWith(".osu")) {
                filename += ".osu";
            }

            return filename;
        }
    }
}