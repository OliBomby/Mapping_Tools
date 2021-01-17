using System;
using System.Collections.Generic;

namespace Mapping_Tools_Core.Audio.DuplicateDetection {
    public interface IDuplicateSampleDetector {
        /// <summary>
        /// Analyses all sound samples in a folder and generates
        /// a mapping from a full path without extension
        /// to the full path of the first sample which makes the same sound.
        /// Use this to detect duplicate samples.
        /// </summary>
        /// <param name="dir">The directory to search in</param>
        /// <param name="includeSubdirectories">Whether to also look in sub-drectories of the directory</param>
        /// <param name="exception">Any exception that may have happened while comparing samples</param>
        /// <returns></returns>
        Dictionary<string, string> AnalyzeSamples(string dir, out Exception exception, bool includeSubdirectories);

        /// <summary>
        /// Gets all the supported file extensions for the sample duplicate search.
        /// </summary>
        /// <returns>Array of all supported file extensions</returns>
        string[] GetSupportedExtensions();
    }
}