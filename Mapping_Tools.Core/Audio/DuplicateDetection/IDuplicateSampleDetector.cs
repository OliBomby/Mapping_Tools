using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Audio.DuplicateDetection;

public interface IDuplicateSampleDetector {
    /// <summary>
    /// Analyses all sound samples and generates a <see cref="IDuplicateSampleMap"/>.
    /// Use this to detect duplicate samples.
    /// </summary>
    /// <param name="samples">The samples to analyze.</param>
    /// <param name="exception">Any exception that may have happened while comparing samples.</param>
    /// <returns>The duplicate sample map.</returns>
    IDuplicateSampleMap AnalyzeSamples(IEnumerable<IBeatmapSetFileInfo> samples, [CanBeNull] out Exception exception);

    /// <summary>
    /// Gets all the supported file extensions for the duplicate sample search.
    /// </summary>
    /// <returns>Array of all supported file extensions.</returns>
    string[] GetSupportedExtensions();
}