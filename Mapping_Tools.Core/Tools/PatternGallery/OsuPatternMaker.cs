using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Class for creating patterns.
/// </summary>
public class OsuPatternMaker {
    /// <summary>
    /// Extra time in milliseconds around patterns for including a wider range of objects in the target beatmap.
    /// </summary>
    public double Padding = 5;

    /// <summary>
    /// Creates a pattern of only the selected hit objects in the beatmap and saves it with the file handler.
    /// This doesn't modify the given beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to create a pattern from.</param>
    /// <param name="fileHandler">The file handler to save the pattern beatmap with.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromSelectedWithSave(IBeatmap beatmap, IOsuPatternFileHandler fileHandler, string name) {
        var osuPattern = FromSelected(beatmap, out var patternBeatmap, name);

        fileHandler.SavePatternBeatmap(patternBeatmap, osuPattern.Filename);

        return osuPattern;
    }

    /// <summary>
    /// Creates a pattern of only the selected hit objects in the beatmap.
    /// This doesn't modify the given beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to create a pattern from.</param>
    /// <param name="patternBeatmap">The created pattern beatmap</param>
    /// <param name="name">The name of the pattern.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromSelected(IBeatmap beatmap, out IBeatmap patternBeatmap, string name) {
        // Check if it has selected objects
        if (!beatmap.HitObjects.Any(h => h.IsSelected)) throw new ArgumentException("No selected hit objects found.", nameof(beatmap));

        // Copy it so the changes dont affect the given beatmap object
        patternBeatmap = beatmap.DeepClone();

        RemoveStoryboard(patternBeatmap);
        RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, patternBeatmap.HitObjects.Where(h => h.IsSelected).ToList());

        return FromBeatmap(patternBeatmap, name);
    }

    /// <summary>
    /// Filters out stuff from the beatmap and creates a pattern of the beatmap and saves the pattern beatmap.
    /// This doesn't modify the given beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to create a pattern from.</param>
    /// <param name="fileHandler">The file handler to save the pattern beatmap with.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <param name="filter">Filter with osu! time code syntax. Ex: "01:12:123(1,2,3) - "</param>
    /// <param name="filterStartTime">Start time of time filter in milliseconds.</param>
    /// <param name="filterEndTime">End time of time filter in milliseconds.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromBeatmapFilterWithSave(IBeatmap beatmap, OsuPatternFileHandler fileHandler, string name,
        string filter = null, double filterStartTime = -1, double filterEndTime = -1) {

        var osuPattern = FromBeatmapFilter(beatmap, out var patternBeatmap, name, filter, filterStartTime, filterEndTime);

        // Save the modified pattern beatmap in the collection folder
        fileHandler.SavePatternBeatmap(patternBeatmap, osuPattern.Filename);

        return osuPattern;
    }

    /// <summary>
    /// Filters out stuff from the beatmap and creates a pattern of the beatmap.
    /// This doesn't modify the given beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to create a pattern from.</param>
    /// <param name="patternBeatmap">The created pattern beatmap.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <param name="filter">Filter with osu! time code syntax. Ex: "01:12:123(1,2,3) - "</param>
    /// <param name="filterStartTime">Start time of time filter in milliseconds.</param>
    /// <param name="filterEndTime">End time of time filter in milliseconds.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromBeatmapFilter(IBeatmap beatmap, out IBeatmap patternBeatmap, string name,
        string filter = null, double filterStartTime = -1, double filterEndTime = -1) {
        // Copy it so the changes dont affect the given beatmap object
        patternBeatmap = beatmap.DeepClone();

        RemoveStoryboard(patternBeatmap);

        // Optionally filter stuff
        var hitObjects = !string.IsNullOrEmpty(filter) ? patternBeatmap.QueryTimeCode(filter).ToList() : patternBeatmap.HitObjects;

        if (filterStartTime != -1) {
            hitObjects.RemoveAll(o => o.EndTime < filterStartTime);
        }
        if (filterEndTime != -1) {
            hitObjects.RemoveAll(o => o.StartTime > filterEndTime);
        }

        RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, hitObjects);

        return FromBeatmap(patternBeatmap, name);
    }

    /// <summary>
    /// Creates a pattern from a collection hit objects and timing points and stores it with the file handler.
    /// </summary>
    /// <param name="fileHandler">The file handler to store the pattern beatmap with.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <param name="hitObjects">The hit objects of the pattern.</param>
    /// <param name="timingPoints">The timing points of the pattern.</param>
    /// <param name="firstUnInheritedTimingPoint">The first uninherited timing point of the pattern. (optional)</param>
    /// <param name="globalSv">The global slider velocity of the pattern.</param>
    /// <param name="gameMode">The game mode of the pattern.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromObjectsWithSave(List<HitObject> hitObjects, List<TimingPoint> timingPoints, IOsuPatternFileHandler fileHandler,
        string name, TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) {

        var osuPattern = FromObjects(hitObjects, timingPoints, out var patternBeatmap, name,
            firstUnInheritedTimingPoint, globalSv, gameMode);

        fileHandler.SavePatternBeatmap(patternBeatmap, osuPattern.Filename);

        return osuPattern;
    }

    /// <summary>
    /// Creates a pattern and pattern beatmap from a collection hit objects and timing points.
    /// </summary>
    /// <param name="patternBeatmap">The created pattern beatmap.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <param name="hitObjects">The hit objects of the pattern.</param>
    /// <param name="timingPoints">The timing points of the pattern.</param>
    /// <param name="firstUnInheritedTimingPoint">The first uninherited timing point of the pattern. (optional)</param>
    /// <param name="globalSv">The global slider velocity of the pattern.</param>
    /// <param name="gameMode">The game mode of the pattern.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromObjects(List<HitObject> hitObjects, List<TimingPoint> timingPoints, out IBeatmap patternBeatmap,
        string name, TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) {
        patternBeatmap = new Beatmap(hitObjects, timingPoints, firstUnInheritedTimingPoint, globalSv, gameMode) {
            Metadata = {Version = name}
        };

        return FromBeatmap(patternBeatmap, name);
    }

    /// <summary>
    /// Creates a pattern from the entire beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to create a pattern from.</param>
    /// <param name="name">The name of the pattern.</param>
    /// <returns>The created pattern.</returns>
    public IOsuPattern FromBeatmap(IBeatmap beatmap, string name) {
        // Import a file name and save the pattern
        var now = DateTime.Now;
        var fileName = GenerateUniquePatternFileName(name, now);

        var startTime = beatmap.GetHitObjectStartTime();
        var endTime = beatmap.GetHitObjectEndTime();

        return new OsuPattern(fileName,
            beatmap.HitObjects.Count,
            TimeSpan.FromMilliseconds(endTime - startTime),
            beatmap.BeatmapTiming.GetBeatLength(startTime, endTime, true)) {
            Name = name,
            CreationTime = now,
            LastUsedTime = now,
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

    private static void RemoveStoryboard(IBeatmap beatmap) {
        // Remove the storyboarding
        beatmap.Storyboard.StoryboardLayerFail.Clear();
        beatmap.Storyboard.StoryboardLayerPass.Clear();
        beatmap.Storyboard.StoryboardLayerBackground.Clear();
        beatmap.Storyboard.StoryboardLayerForeground.Clear();
        beatmap.Storyboard.StoryboardLayerOverlay.Clear();
    }

    private void RemoveEverythingThatIsNotTheseHitObjects(IBeatmap beatmap, List<HitObject> hitObjects) {
        // Keep the selected subset of hit objects
        beatmap.HitObjects = hitObjects;

        var startTime = beatmap.GetHitObjectStartTime() - Padding;
        var endTime = beatmap.GetHitObjectEndTime() + Padding;

        // Keep the timing points in the range of the hitobjects
        beatmap.BeatmapTiming.RemoveAll(tp => !(tp.Offset >= startTime && tp.Offset <= endTime));

        // Add some earlier timing points if necessary
        var first = beatmap.HitObjects.First();
        TimingPoint firstUnInheritedTimingPoint;
        TimingPoint firstNormalTimingPoint;
        if (first.TryGetContext<TimingContext>(out var firstTiming)) {
            firstUnInheritedTimingPoint = firstTiming.UninheritedTimingPoint;
            firstNormalTimingPoint = firstTiming.TimingPoint;
        } else {
            firstUnInheritedTimingPoint = beatmap.BeatmapTiming.GetRedlineAtTime(first.StartTime);
            firstNormalTimingPoint = beatmap.BeatmapTiming.GetTimingPointAtTime(first.StartTime);
        }

        if (!beatmap.BeatmapTiming.Contains(firstUnInheritedTimingPoint)) {
            beatmap.BeatmapTiming.Add(firstUnInheritedTimingPoint);
        }
        if (!beatmap.BeatmapTiming.Contains(firstNormalTimingPoint)) {
            beatmap.BeatmapTiming.Add(firstNormalTimingPoint);
        }
    }

    #endregion
}