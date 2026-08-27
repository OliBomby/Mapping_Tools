using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.MapsetMerger.Models;

namespace Mapping_Tools.Core.Tools.MapsetMerger;

/// <summary>
///     Applies Mapset Merger's document-only reference and conflict rules without
///     reading or writing files.
/// </summary>
public static partial class MapsetMergerEngine
{
    private static readonly StringComparer nameComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    ///     Renames repeated mapset names by appending the first available positive
    ///     integer, preserving the order in which inputs were supplied.
    /// </summary>
    /// <param name="mapsets">The mutable inputs to normalize.</param>
    /// <exception cref="ArgumentNullException">An input collection or item is null.</exception>
    /// <exception cref="ArgumentException">A mapset name is blank or unsafe as a path segment.</exception>
    public static void ResolveDuplicateMapsetNames(IList<MapsetMergerInput> mapsets)
    {
        ArgumentNullException.ThrowIfNull(mapsets);
        HashSet<string> used = new(nameComparer);

        foreach (var mapset in mapsets)
        {
            ArgumentNullException.ThrowIfNull(mapset);
            RequireSafeMapsetName(mapset.Name);
            string original = mapset.Name;
            string candidate = original;
            int suffix = 0;
            while (!used.Add(candidate)) candidate = original + ++suffix;

            mapset.Name = candidate;
        }
    }

    /// <summary>
    ///     Validates the source mapset inputs before they are used to build output paths.
    /// </summary>
    /// <param name="mapsets">The source mapsets in their requested merge order.</param>
    /// <exception cref="ArgumentNullException">The collection or an input item is null.</exception>
    /// <exception cref="ArgumentException">A mapset name or source path is blank or unsafe.</exception>
    public static void Validate(IReadOnlyList<MapsetMergerInput> mapsets)
    {
        ArgumentNullException.ThrowIfNull(mapsets);
        if (mapsets.Count == 0) throw new ArgumentException("Add at least one mapset.", nameof(mapsets));

        foreach (var mapset in mapsets)
        {
            ArgumentNullException.ThrowIfNull(mapset);
            RequireSafeMapsetName(mapset.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(mapset.Path);
        }
    }

    /// <summary>
    ///     Selects a unique difficulty name and records it in the supplied set.
    /// </summary>
    /// <param name="requestedName">The metadata version from the source beatmap.</param>
    /// <param name="prefix">The mapset prefix used for conflicts.</param>
    /// <param name="usedNames">Names already emitted in the merged mapset.</param>
    /// <returns>A unique metadata version.</returns>
    public static string ResolveDuplicateDifficultyName(
        string requestedName,
        string prefix,
        ISet<string> usedNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(usedNames);

        string candidate = requestedName;
        if (!usedNames.Contains(candidate))
        {
            usedNames.Add(candidate);
            return candidate;
        }

        int suffix = 0;
        do
        {
            candidate = prefix + requestedName + ++suffix;
        } while (!usedNames.Add(candidate));

        return candidate;
    }

    /// <summary>
    ///     Rewrites all beatmap-owned references and remaps explicit custom sample
    ///     indices for one source mapset.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap to rewrite.</param>
    /// <param name="mapsetName">The output folder name for this source.</param>
    /// <param name="nextSampleIndex">The next global custom index.</param>
    /// <param name="sampleIndices">The source-to-output index map for this mapset.</param>
    /// <returns>The assets referenced by the rewritten beatmap.</returns>
    public static MapsetMergerReferences RewriteBeatmapReferences(
        Beatmap beatmap,
        string mapsetName,
        ref int nextSampleIndex,
        IDictionary<int, int> sampleIndices)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        RequireSafeMapsetName(mapsetName);
        ArgumentNullException.ThrowIfNull(sampleIndices);
        if (nextSampleIndex < 1) throw new ArgumentOutOfRangeException(nameof(nextSampleIndex));

        MapsetMergerReferences references = new();
        string audioFilename = beatmap.General["AudioFilename"].Value.Trim();
        if (!string.IsNullOrEmpty(audioFilename))
        {
            references.OtherAudioFiles.Add(audioFilename);
            beatmap.General["AudioFilename"].Value = " " + CombineReference(mapsetName, audioFilename);
        }

        double sliderTickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;
        // All hitsound files with custom indices
        foreach (var hitObject in beatmap.HitObjects)
            references.HitSoundFiles.UnionWith(
                hitObject.GetPlayingBodyFilenames(sliderTickRate, false));

        var mode = (GameMode)beatmap.General["Mode"].IntValue;
        var timeline = beatmap.GetTimeline();
        // All explicitly referenced audio files like filename hs, SB samples
        foreach (var timelineObject in timeline.TimelineObjects)
        foreach (string filename in timelineObject.GetPlayingFilenames(mode, false))
            if (!string.IsNullOrEmpty(filename) && string.Equals(filename, timelineObject.Filename, StringComparison.Ordinal))
            {
                references.OtherAudioFiles.Add(filename);
                timelineObject.Filename = CombineReference(mapsetName, filename);
                timelineObject.HitsoundsToOrigin();
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                references.HitSoundFiles.Add(filename);
            }

        // All hitsound indices in the beatmaps. Old index to new index
        // Adjust the remaining custom indices
        foreach (var hitObject in beatmap.HitObjects) hitObject.CustomIndex = RemapIndex(hitObject.CustomIndex, sampleIndices, ref nextSampleIndex);

        foreach (var timingPoint in beatmap.BeatmapTiming.TimingPoints)
            timingPoint.SampleIndex = RemapIndex(
                timingPoint.SampleIndex,
                sampleIndices,
                ref nextSampleIndex);

        return references;
    }

    /// <summary>
    ///     Rewrites storyboard event references recursively and returns their asset set.
    /// </summary>
    /// <param name="storyboard">The storyboard to rewrite.</param>
    /// <param name="mapsetName">The output folder name for this source.</param>
    /// <returns>The assets referenced by the rewritten storyboard.</returns>
    public static MapsetMergerReferences RewriteStoryboardReferences(
        StoryBoard storyboard,
        string mapsetName)
    {
        ArgumentNullException.ThrowIfNull(storyboard);
        RequireSafeMapsetName(mapsetName);
        MapsetMergerReferences references = new();

        IEnumerable<Event> events = storyboard.BackgroundAndVideoEvents
            .Concat(storyboard.StoryboardSoundSamples)
            .Concat(storyboard.StoryboardLayerFail)
            .Concat(storyboard.StoryboardLayerPass)
            .Concat(storyboard.StoryboardLayerBackground)
            .Concat(storyboard.StoryboardLayerForeground)
            .Concat(storyboard.StoryboardLayerOverlay);
        RewriteEvents(events, mapsetName, references);
        return references;
    }

    /// <summary>Combines an output folder with a beatmap-relative reference.</summary>
    /// <param name="mapsetName">The validated output folder.</param>
    /// <param name="reference">The relative osu! reference.</param>
    /// <returns>The combined reference using platform path semantics.</returns>
    public static string CombineReference(string mapsetName, string reference)
    {
        RequireSafeMapsetName(mapsetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (Path.IsPathRooted(reference) || reference.Split('/', '\\').Any(part => part is ".."))
            throw new InvalidDataException($"The asset reference '{reference}' is not relative.");

        return Path.Combine(mapsetName, reference);
    }

    private static void RequireSafeMapsetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            throw new ArgumentException(
                "Mapset names must be one safe filesystem path segment.",
                nameof(name));
    }

    private static int RemapIndex(
        int index,
        IDictionary<int, int> sampleIndices,
        ref int nextSampleIndex)
    {
        if (index == 0) return 0;

        if (!sampleIndices.TryGetValue(index, out int mapped))
        {
            mapped = nextSampleIndex++;
            sampleIndices[index] = mapped;
        }

        return mapped;
    }

    private static void RewriteEvents(
        IEnumerable<Event> events,
        string mapsetName,
        MapsetMergerReferences references)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case StoryboardSoundSample sample:
                    references.OtherAudioFiles.Add(sample.FilePath);
                    sample.FilePath = CombineReference(mapsetName, sample.FilePath);
                    break;
                case Animation animation:
                    string animationDirectory = Path.GetDirectoryName(animation.FilePath) ?? string.Empty;
                    string animationStem = Path.GetFileNameWithoutExtension(animation.FilePath);
                    for (int index = 0; index < animation.FrameCount; index++)
                        references.ImageFiles.Add(
                            Path.Combine(animationDirectory, animationStem + index));

                    animation.FilePath = CombineReference(mapsetName, animation.FilePath);
                    break;
                case Sprite sprite:
                    references.ImageFiles.Add(sprite.FilePath);
                    sprite.FilePath = CombineReference(mapsetName, sprite.FilePath);
                    break;
                case Background background:
                    references.ImageFiles.Add(background.Filename);
                    background.Filename = CombineReference(mapsetName, background.Filename);
                    break;
                case Video video:
                    references.VideoFiles.Add(video.Filename);
                    video.Filename = CombineReference(mapsetName, video.Filename);
                    break;
            }

            if (@event.ChildEvents.Count > 0) RewriteEvents(@event.ChildEvents, mapsetName, references);
        }
    }

    [GeneratedRegex("^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide|sliderwhistle)", RegexOptions.IgnoreCase)]
    private static partial Regex HitsoundSampleFilenameRegex();

    /// <summary>
    ///     Converts a source custom sample filename into its output filename while
    ///     retaining the legacy index-one/no-suffix convention.
    /// </summary>
    /// <param name="filename">The source filename without or with an extension.</param>
    /// <param name="mappedIndex">The remapped custom index.</param>
    /// <returns>The output filename stem and original extension.</returns>
    public static string GetRemappedHitsoundFilename(string filename, int mappedIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        if (mappedIndex < 1) throw new ArgumentOutOfRangeException(nameof(mappedIndex));

        string extension = Path.GetExtension(filename);
        string extensionless = Path.GetFileNameWithoutExtension(filename);
        var match = HitsoundSampleFilenameRegex().Match(extensionless);
        if (!match.Success) return Path.GetFileName(filename);

        string suffix = mappedIndex == 1 ? string.Empty : mappedIndex.ToString();
        return match.Value + suffix + extension;
    }
}
