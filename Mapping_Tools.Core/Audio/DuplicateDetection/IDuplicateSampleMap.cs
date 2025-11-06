using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Audio.DuplicateDetection;

/// <summary>
/// Equality comparer for finding duplicate samples.
/// </summary>
public interface IDuplicateSampleMap {
    /// <summary>
    /// Gets the first sample in the equality class of given sample.
    /// </summary>
    /// <param name="sample">The sample to get the equality class of.</param>
    /// <returns>The first sample in the equality class.</returns>
    [CanBeNull]
    IBeatmapSetFileInfo GetOriginalSample(IBeatmapSetFileInfo sample);

    /// <summary>
    /// Gets the first sample in the equality class of given sample.
    /// </summary>
    /// <param name="filename">The filename of the sample to get the equality class of.</param>
    /// <returns>The first sample in the equality class or null if this file doesn't exist.</returns>
    [CanBeNull]
    IBeatmapSetFileInfo GetOriginalSample(string filename);

    /// <summary>
    /// Checks if the given sample exists in the sample map.
    /// </summary>
    /// <param name="sample">The sample to check.</param>
    /// <returns>Whether given sample exists in the sample map.</returns>
    bool SampleExists(IBeatmapSetFileInfo sample);

    /// <summary>
    /// Checks if the given sample exists in the sample map.
    /// </summary>
    /// <param name="filename">The filename of the sample to check.</param>
    /// <returns>Whether given sample exists in the sample map.</returns>
    bool SampleExists(string filename);

    /// <summary>
    /// Checks if two samples make the same sound.
    /// </summary>
    /// <param name="sample1">The first sample.</param>
    /// <param name="sample2">The second sample.</param>
    /// <returns>Whether both samples make the same sound.</returns>
    bool IsDuplicate(IBeatmapSetFileInfo sample1, IBeatmapSetFileInfo sample2);

    /// <summary>
    /// Checks if two samples make the same sound.
    /// </summary>
    /// <param name="filename1">The filename of the first sample.</param>
    /// <param name="filename2">The filename of the second sample.</param>
    /// <returns>Whether both samples make the same sound.</returns>
    bool IsDuplicate(string filename1, string filename2);
}