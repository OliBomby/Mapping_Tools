using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
///     Extracts indexed patterns from beatmaps without performing filesystem I/O.
/// </summary>
public sealed class PatternGalleryMaker
{
    /// <summary>Gets or sets the timing margin retained around extracted objects.</summary>
    public double Padding { get; set; } = 5;

    /// <summary>
    ///     Creates a pattern from an explicit editor selection snapshot.
    /// </summary>
    /// <param name="beatmap">The source map; it is deep-copied before filtering.</param>
    /// <param name="selectedHitObjects">The selected objects from the editor session.</param>
    /// <param name="name">The display name to index.</param>
    /// <param name="patternBeatmap">Receives the filtered standalone map.</param>
    /// <returns>The indexed pattern metadata.</returns>
    /// <exception cref="InvalidOperationException">No objects are selected.</exception>
    public PatternGalleryPattern FromSelected(
        Beatmap beatmap,
        string name,
        IReadOnlyCollection<HitObject> selectedHitObjects,
        out Beatmap patternBeatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(selectedHitObjects);
        var selectedSet = selectedHitObjects.ToHashSet();
        int[] selectedIndices = beatmap.HitObjects
            .Select((item, index) => selectedSet.Contains(item) ? index : -1)
            .Where(index => index >= 0)
            .ToArray();
        // Check if it has selected objects
        if (selectedIndices.Length == 0) throw new InvalidOperationException("No selected hit objects found.");

        // Copy it so the changes dont affect the given beatmap object
        patternBeatmap = beatmap.DeepCopy();
        // Remove the storyboarding
        RemoveStoryboard(patternBeatmap);
        // Keep the selected subset of hit objects
        RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, patternBeatmap.HitObjects
            .Where((item, index) => selectedIndices.Contains(index))
            .ToList());
        return FromBeatmap(patternBeatmap, name);
    }

    /// <summary>Creates a pattern from a parsed source map, optionally filtering its objects.</summary>
    /// <param name="beatmap">The parsed source beatmap.</param>
    /// <param name="name">The display name to index.</param>
    /// <param name="filter">An optional legacy time-code query.</param>
    /// <param name="startTime">Optional inclusive lower object bound in milliseconds.</param>
    /// <param name="endTime">Optional inclusive upper object bound in milliseconds.</param>
    /// <param name="patternBeatmap">Receives the filtered standalone map.</param>
    /// <returns>The indexed pattern metadata.</returns>
    public PatternGalleryPattern FromBeatmapFiltered(
        Beatmap beatmap,
        string name,
        string? filter,
        double startTime,
        double endTime,
        out Beatmap patternBeatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        // Copy it so the changes dont affect the given beatmap object
        patternBeatmap = beatmap.DeepCopy();
        // Remove the storyboarding
        RemoveStoryboard(patternBeatmap);
        // Optionally filter stuff
        List<HitObject> objects = string.IsNullOrEmpty(filter)
            ? patternBeatmap.HitObjects
            : patternBeatmap.QueryTimeCode(filter).ToList();

        if (startTime != -1) objects.RemoveAll(item => item.EndTime < startTime);
        if (endTime != -1) objects.RemoveAll(item => item.Time > endTime);

        if (objects.Count == 0) throw new InvalidOperationException("At least one valid hit object is required.");

        RemoveEverythingThatIsNotTheseHitObjects(patternBeatmap, objects);
        return FromBeatmap(patternBeatmap, name);
    }

    /// <summary>Creates metadata for a complete parsed pattern map.</summary>
    /// <param name="beatmap">The parsed pattern map.</param>
    /// <param name="name">The display name to index.</param>
    /// <param name="filename">An existing filename to retain, or <see langword="null" /> to generate one.</param>
    /// <returns>The metadata record.</returns>
    public PatternGalleryPattern FromBeatmap(Beatmap beatmap, string name, string? filename = null)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (beatmap.HitObjects.Count == 0) throw new InvalidOperationException("A pattern must contain at least one hit object.");

        var now = DateTime.Now;
        double startTime = beatmap.GetHitObjectStartTime();
        double endTime = beatmap.GetHitObjectEndTime();
        return new PatternGalleryPattern
        {
            Name = name,
            CreationTime = now,
            LastUsedTime = now,
            FileName = filename ?? GenerateUniquePatternFileName(name, now),
            ObjectCount = beatmap.HitObjects.Count,
            Duration = TimeSpan.FromMilliseconds(endTime - startTime),
            BeatLength = beatmap.BeatmapTiming.GetBeatLength(startTime, endTime, true),
        };
    }

    /// <summary>Builds a pattern map from raw hit-object and timing-point lines.</summary>
    /// <param name="hitObjects">Valid osu! hit-object lines.</param>
    /// <param name="timingPoints">Valid osu! timing-point lines.</param>
    /// <param name="name">The display name to index.</param>
    /// <param name="globalSv">The source map's global slider multiplier.</param>
    /// <param name="gameMode">The source map game mode.</param>
    /// <param name="patternBeatmap">Receives the constructed map.</param>
    /// <returns>The indexed metadata.</returns>
    public PatternGalleryPattern FromObjects(
        IReadOnlyList<HitObject> hitObjects,
        IReadOnlyList<TimingPoint> timingPoints,
        string name,
        double globalSv,
        GameMode gameMode,
        out Beatmap patternBeatmap)
    {
        ArgumentNullException.ThrowIfNull(hitObjects);
        ArgumentNullException.ThrowIfNull(timingPoints);
        if (hitObjects.Count == 0) throw new InvalidOperationException("At least one valid hit object is required.");

        patternBeatmap = new Beatmap(
            hitObjects.ToList(),
            timingPoints.ToList(),
            null,
            globalSv,
            gameMode)
        {
            Metadata = { ["Version"] = new StringValue(name) },
        };
        return FromBeatmap(patternBeatmap, name);
    }

    private static string GenerateUniquePatternFileName(string name, DateTime time)
    {
        string fileName = $"{time:yyyy-MM-dd HH-mm-ss}_{RNG.RandomString(8)}__{name}";
        if (!fileName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)) fileName += ".osu";

        // Remove invalid characters
        string invalidCharacters = new(Path.GetInvalidFileNameChars());
        return Regex.Replace(fileName, $"[{Regex.Escape(invalidCharacters)}]", string.Empty);
    }

    private static void RemoveStoryboard(Beatmap beatmap)
    {
        beatmap.StoryboardLayerFail.Clear();
        beatmap.StoryboardLayerPass.Clear();
        beatmap.StoryboardLayerBackground.Clear();
        beatmap.StoryboardLayerForeground.Clear();
        beatmap.StoryboardLayerOverlay.Clear();
    }

    private void RemoveEverythingThatIsNotTheseHitObjects(Beatmap beatmap, List<HitObject> hitObjects)
    {
        beatmap.HitObjects = hitObjects;
        double startTime = beatmap.GetHitObjectStartTime() - Padding;
        double endTime = beatmap.GetHitObjectEndTime() + Padding;
        // Keep the timing points in the range of the hitobjects
        beatmap.BeatmapTiming.RemoveAll(point => point.Offset < startTime || point.Offset > endTime);

        // Add some earlier timing points if necessary
        var firstUninherited = beatmap.HitObjects[0].UnInheritedTimingPoint;
        var firstNormal = beatmap.HitObjects[0].TimingPoint;
        if (!beatmap.BeatmapTiming.Contains(firstUninherited)) beatmap.BeatmapTiming.Add(firstUninherited);
        if (!beatmap.BeatmapTiming.Contains(firstNormal)) beatmap.BeatmapTiming.Add(firstNormal);
    }
}
