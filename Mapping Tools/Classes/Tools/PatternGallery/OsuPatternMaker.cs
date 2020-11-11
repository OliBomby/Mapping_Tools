using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternMaker {
        /// <summary>
        /// Extra time in milliseconds around patterns for including a wider range of objects in the target beatmap.
        /// </summary>
        public double Padding = 5;

        public OsuPattern FromSelectedWithSave(Beatmap beatmap, OsuPatternFileHandler fileHandler, string name) {
            var osuPattern = FromSelected(beatmap, out var patternBeatmap, name);

            patternBeatmap.SaveWithFloatPrecision = true;
            Editor.SaveFile(fileHandler.GetPatternPath(osuPattern.FileName), patternBeatmap.GetLines());

            return osuPattern;
        }

        public OsuPattern FromSelected(Beatmap beatmap, out Beatmap patternBeatmap, string name) {
            // Check if it has selected objects
            if (!beatmap.HitObjects.Any(h => h.IsSelected)) throw new Exception("No selected hit objects found.");

            // Copy it so the changes dont affect the given beatmap object
            patternBeatmap = beatmap.DeepCopy();

            RemoveStoryboard(patternBeatmap);
            RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, patternBeatmap.HitObjects.Where(h => h.IsSelected).ToList());

            return FromBeatmap(patternBeatmap, name);
        }

        public OsuPattern FromFileWithSave(string filePath, OsuPatternFileHandler fileHandler, string name, 
            string filter = null, double filterStartTime = -1, double filterEndTime = -1) {

            OsuPattern osuPattern;
            if (!string.IsNullOrEmpty(filter) || filterStartTime != -1 || filterEndTime != -1) {
                osuPattern = FromFileFilter(filePath, out var patternBeatmap, name, filter, filterStartTime, filterEndTime);

                // Save the modified pattern beatmap in the colleciton folder
                var newFilePath = fileHandler.GetPatternPath(osuPattern.FileName);
                patternBeatmap.SaveWithFloatPrecision = true;
                Editor.SaveFile(newFilePath, patternBeatmap.GetLines());
            }
            else {
                osuPattern = FromFile(filePath, name);

                // Save the pattern in the collection folder by copying
                var newFilePath = fileHandler.GetPatternPath(osuPattern.FileName);
                File.Copy(filePath, newFilePath, false);
            }

            return osuPattern;
        }

        public OsuPattern FromFileFilter(string filePath, out Beatmap patternBeatmap, string name, 
            string filter = null, double filterStartTime = -1, double filterEndTime = -1) {
            // Read some stuff from the pattern
            patternBeatmap = new BeatmapEditor(filePath).Beatmap;

            RemoveStoryboard(patternBeatmap);

            // Optionally filter stuff
            var hitObjects = !string.IsNullOrEmpty(filter) ? patternBeatmap.QueryTimeCode(filter).ToList() : patternBeatmap.HitObjects;

            if (filterStartTime != -1) {
                hitObjects.RemoveAll(o => o.EndTime < filterStartTime);
            }
            if (filterEndTime != -1) {
                hitObjects.RemoveAll(o => o.Time > filterEndTime);
            }

            RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, hitObjects);

            return FromBeatmap(patternBeatmap, name);
        }

        public OsuPattern FromFile(string filePath, string name) {
            // Read some stuff from the pattern
            var patternBeatmap = new BeatmapEditor(filePath).Beatmap;

            return FromBeatmap(patternBeatmap, name);
        }

        public OsuPattern FromObjectsWithSave(List<HitObject> hitObjects, List<TimingPoint> timingPoints, OsuPatternFileHandler fileHandler,
            string name, TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) {
            var osuPattern = FromObjects(hitObjects, timingPoints, out var patternBeatmap, name,
                firstUnInheritedTimingPoint, globalSv, gameMode);

            patternBeatmap.SaveWithFloatPrecision = true;
            Editor.SaveFile(fileHandler.GetPatternPath(osuPattern.FileName), patternBeatmap.GetLines());

            return osuPattern;
        }

        public OsuPattern FromObjects(List<HitObject> hitObjects, List<TimingPoint> timingPoints, out Beatmap patternBeatmap, 
            string name, TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) {
            patternBeatmap = new Beatmap(hitObjects, timingPoints, firstUnInheritedTimingPoint, globalSv, gameMode) {
                    Metadata = {["Version"] = new TValue(name)}
                };

            return FromBeatmap(patternBeatmap, name);
        }

        public OsuPattern FromBeatmap(Beatmap beatmap, string name) {
            // Generate a file name and save the pattern
            var now = DateTime.Now;
            var fileName = GenerateUniquePatternFileName(name, now);

            var startTime = beatmap.GetHitObjectStartTime();
            var endTime = beatmap.GetHitObjectEndTime();

            return new OsuPattern {
                Name = name,
                CreationTime = now,
                LastUsedTime = now,
                FileName = fileName,
                ObjectCount = beatmap.HitObjects.Count,
                Duration = TimeSpan.FromMilliseconds(endTime - startTime),
                BeatLength = beatmap.BeatmapTiming.GetBeatLength(startTime, endTime, true)
            };
        }

        #region Helpers

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

        private static void RemoveStoryboard(Beatmap beatmap) {
            // Remove the storyboarding
            beatmap.StoryboardLayerFail.Clear();
            beatmap.StoryboardLayerPass.Clear();
            beatmap.StoryboardLayerBackground.Clear();
            beatmap.StoryboardLayerForeground.Clear();
            beatmap.StoryboardLayerOverlay.Clear();
        }

        private void RemoveEverythingThatIsNotTheseHitObjects(Beatmap beatmap, List<HitObject> hitObjects) {
            // Keep the selected subset of hit objects
            beatmap.HitObjects = hitObjects;

            var startTime = beatmap.GetHitObjectStartTime() - Padding;
            var endTime = beatmap.GetHitObjectEndTime() + Padding;

            // Keep the timing points in the range of the hitobjects
            beatmap.BeatmapTiming.RemoveAll(tp => !(tp.Offset >= startTime && tp.Offset <= endTime));

            // Add some earlier timing points if necessary
            var firstUnInheritedTimingPoint = beatmap.HitObjects.First().UnInheritedTimingPoint;
            var firstNormalTimingPoint = beatmap.HitObjects.First().TimingPoint;

            if (!beatmap.BeatmapTiming.Contains(firstUnInheritedTimingPoint)) {
                beatmap.BeatmapTiming.Add(firstUnInheritedTimingPoint);
            }
            if (!beatmap.BeatmapTiming.Contains(firstNormalTimingPoint)) {
                beatmap.BeatmapTiming.Add(firstNormalTimingPoint);
            }
        }

        #endregion
    }
}