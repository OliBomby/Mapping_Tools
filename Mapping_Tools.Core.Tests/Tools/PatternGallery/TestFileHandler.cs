using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Core.Tests.Tools.PatternGallery;

public class TestFileHandler : IOsuPatternFileHandler {
    private readonly Dictionary<string, IBeatmap> beatmaps = new Dictionary<string, IBeatmap>();

    public IBeatmap GetPatternBeatmap(string filename) {
        if (beatmaps.TryGetValue(filename, out var value)) {
            return value;
        }

        throw new FileNotFoundException();
    }

    public void SavePatternBeatmap(IBeatmap beatmap, string filename) {
        beatmaps[filename] = beatmap;
    }
}