using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Audio.DuplicateDetection;

/// <summary>
/// Duplicate detector that doesnt actually detect duplicates.
/// It just creates a <see cref="IDuplicateSampleMap"/> with all the samples and no duplicates.
/// </summary>
public class UselessDuplicateSampleDetector : IDuplicateSampleDetector {
    public IDuplicateSampleMap AnalyzeSamples(IEnumerable<IBeatmapSetFileInfo> samples, out Exception exception) {
        var extList = GetSupportedExtensions();
        exception = null;

        List<IBeatmapSetFileInfo> samplesFiltered = samples
            .Where(n => extList.Contains(Path.GetExtension(n.Filename), StringComparer.OrdinalIgnoreCase)).ToList();

        Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo> dict = new Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo>();

        foreach (var sample in samplesFiltered) {
            dict[sample] = sample;
        }

        return new DictionaryDuplicateSampleMap(dict);
    }

    public string[] GetSupportedExtensions() {
        return new[] { ".wav", ".aif", ".aiff", ".ogg", ".mp3" };
    }
}