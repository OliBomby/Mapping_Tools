using Mapping_Tools_Core.BeatmapHelper.Decoding;
using Mapping_Tools_Core.BeatmapHelper.Encoding;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// Editor specifically for storyboards
    /// </summary>
    public class StoryboardEditor : Editor<Storyboard> {
        public StoryboardEditor() : base(new OsuStoryboardEncoder(), new OsuStoryboardDecoder()) {}

        public StoryboardEditor(string path) : base(new OsuStoryboardEncoder(), new OsuStoryboardDecoder(), path) {}
    }
}
