using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Audio.DuplicateDetection;

public class DictionaryDuplicateSampleMap : IDuplicateSampleMap {
    /// <summary>
    /// Maps each sample to the first sample in the equality class.
    /// </summary>
    public Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo> Map { get; }

    public DictionaryDuplicateSampleMap() : this(new Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo>()) { }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="map">Dictionary that maps each sample to the first sample in the equality class.</param>
    public DictionaryDuplicateSampleMap(Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo> map) {
        Map = map;
    }

    public IBeatmapSetFileInfo GetOriginalSample(IBeatmapSetFileInfo sample) {
        if (sample == null)
            return null;

        return Map.TryGetValue(sample, out var original) ? original : null;
    }

    public IBeatmapSetFileInfo GetOriginalSample(string filename) {
        return GetOriginalSample(GetByFilename(filename));
    }

    public bool SampleExists(IBeatmapSetFileInfo sample) {
        return sample != null && Map.ContainsKey(sample);
    }

    public bool SampleExists(string filename) {
        return SampleExists(GetByFilename(filename));
    }

    public bool IsDuplicate(IBeatmapSetFileInfo sample1, IBeatmapSetFileInfo sample2) {
        var class1 = GetOriginalSample(sample1);
        var class2 = GetOriginalSample(sample2);

        return class1 != null && class2 != null && class1.Equals(class2);
    }

    public bool IsDuplicate(string filename1, string filename2) {
        return IsDuplicate(GetByFilename(filename1), GetByFilename(filename2));
    }

    private IBeatmapSetFileInfo GetByFilename(string filename) {
        return BeatmapSetInfo.GetSoundFile(Map.Keys, filename);
    }
}