using System.IO;
using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools_Core.Tools.PatternGallery {
    public interface IOsuPatternFileHandler {
        IBeatmap GetPatternBeatmap(string filename);

        //TODO: use IBeatmap here
        void SavePatternBeatmap(Beatmap beatmap, string filename);
    }
}