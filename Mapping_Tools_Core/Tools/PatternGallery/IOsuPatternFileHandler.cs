using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools_Core.Tools.PatternGallery {
    public interface IOsuPatternFileHandler {
        IBeatmap GetPatternBeatmap(string filename);

        void SavePatternBeatmap(IBeatmap beatmap, string filename);
    }
}