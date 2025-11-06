using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
/// Contains all the contents of a beatmap set.
/// </summary>
public class BeatmapSetInfo {
    private static readonly string[] soundExtensions = { ".wav", ".ogg", ".mp3" };

    /// <summary>
    /// All the files in this beatmap set.
    /// </summary>
    [CanBeNull]
    public List<IBeatmapSetFileInfo> Files { get; set; }

    /// <summary>
    /// Dictionary of pairs (relative path, beatmap) that has all the beatmaps of this beatmap set.
    /// </summary>
    [CanBeNull]
    public Dictionary<string, IBeatmap> Beatmaps { get; set; }

    /// <summary>
    /// Dictionary of pairs (relative path, storyboard) that has all the storyboards of this beatmap set.
    /// </summary>
    [CanBeNull]
    public Dictionary<string, IStoryboard> Storyboards { get; set; }

    /// <summary>
    /// All the sound files in this beatmap set.
    /// </summary>
    [CanBeNull]
    public IEnumerable<IBeatmapSetFileInfo> SoundFiles => Files?.Where(f =>
        soundExtensions.Contains(Path.GetExtension(f.Filename), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the relative path to a beatmap in the beatmap set.
    /// </summary>
    /// <param name="beatmap">The beatmap to get the relative path of.</param>
    /// <returns>The relative path to the beatmap or null.</returns>
    [CanBeNull]
    public string GetRelativePath(IBeatmap beatmap) {
        if (Beatmaps == null)
            return null;

        foreach (var (path, beatmap1) in Beatmaps) {
            if (beatmap1.Equals(beatmap)) {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the sound file with a given filename.
    /// </summary>
    /// <param name="filename">The filename to search.</param>
    /// <returns>The sound file with the filename or null if nothing was found.</returns>
    public IBeatmapSetFileInfo GetSoundFile(string filename) {
        return GetSoundFile(SoundFiles, filename);
    }

    /// <summary>
    /// Finds the sound file with a given filename.
    /// </summary>
    /// <param name="samples">The sound files to search.</param>
    /// <param name="filename">The filename to search.</param>
    /// <returns>The sound file with the filename or null if nothing was found.</returns>
    public static IBeatmapSetFileInfo GetSoundFile(IEnumerable<IBeatmapSetFileInfo> samples, string filename) {
        if (samples == null)
            return null;

        var extension = Path.GetExtension(filename);
        var extensionOrder = soundExtensions.ToList();

        IBeatmapSetFileInfo bestMatch = null;
        int matchLevel = int.MaxValue;
        foreach (var sample in samples) {
            // Minimum requirement for matching filename
            if (Path.ChangeExtension(filename, null) == Path.ChangeExtension(sample.Filename, null)) {
                // Get the index of the extension in the extension priority list
                var extensionLevel = extensionOrder.IndexOf(Path.GetExtension(sample.Filename));
                if (extensionLevel < 0) {
                    extensionLevel = int.MaxValue;
                }

                if (bestMatch == null) {
                    bestMatch = sample;
                    matchLevel = extensionLevel;
                } else if (extensionLevel < matchLevel) {
                    bestMatch = sample;
                    matchLevel = extensionLevel;
                }

                if (!string.IsNullOrEmpty(extension) && Path.GetExtension(bestMatch.Filename) == extension ||
                    string.IsNullOrEmpty(extension) && matchLevel == 0) {
                    break;
                }
            }
        }

        return bestMatch;
    }
}