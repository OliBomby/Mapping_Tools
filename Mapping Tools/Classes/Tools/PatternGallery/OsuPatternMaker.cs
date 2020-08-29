using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternMaker {
        public double Padding { get; set; } = 5;

        public OsuPattern FromSelectedWithSave(Beatmap beatmap, string name, OsuPatternFileHandler fileHandler) {
            var osuPattern = FromSelected(beatmap, name, out var patternBeatmap);

            // Could possibly be saved async
            patternBeatmap.SaveWithFloatPrecision = true;
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

            var startTime = patternBeatmap.GetHitObjectStartTime() - Padding;
            var endTime = patternBeatmap.GetHitObjectEndTime() + Padding;

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
            var now = DateTime.Now;
            var fileName = GenerateUniquePatternFileName(name, now);

            return new OsuPattern {
                Name = name,
                CreationTime = now,
                LastUsedTime = now,
                FileName = fileName,
                ObjectCount = patternBeatmap.HitObjects.Count,
                Duration = TimeSpan.FromMilliseconds(endTime - startTime - 2 * Padding),
                BeatLength = patternBeatmap.BeatmapTiming.GetBeatLength(startTime + Padding, endTime - Padding, true)
            };
        }

        private static string GenerateUniquePatternFileName(string name, DateTime time) {
            var fileName = time.ToString("yyyy-MM-dd HH-mm-ss") + "_" + RNG.RandomString(8) + "__" + name;

            if (!fileName.EndsWith(".osu")) {
                fileName += ".osu";
            }

            // Remove invalid characters
            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
            fileName = r.Replace(fileName, "");

            return fileName;
        }
    }
}