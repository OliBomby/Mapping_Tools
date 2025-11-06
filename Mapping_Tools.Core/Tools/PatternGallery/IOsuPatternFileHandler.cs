using System.IO;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// File interface for mapping patterns.
/// </summary>
public interface IOsuPatternFileHandler {
    /// <summary>
    /// Gets the pattern beatmap with the specified filename.
    /// </summary>
    /// <param name="filename">The filename of the pattern beatmap.</param>
    /// <returns>The beatmap with the pattern data.</returns>
    /// <exception cref="FileNotFoundException">If the beatmap file could not be found.</exception>
    IBeatmap GetPatternBeatmap(string filename);

    /// <summary>
    /// Saves a pattern beatmap with a filename.
    /// </summary>
    /// <param name="beatmap">The pattern beatmap to save.</param>
    /// <param name="filename">The filename location for the pattern beatmap.</param>
    void SavePatternBeatmap(IBeatmap beatmap, string filename);
}