using Mapping_Tools_Core.BeatmapHelper.Decoding;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// Editor specifically for storyboards
    /// </summary>
    public class StoryboardEditor : Editor<Storyboard> {
        public StoryboardEditor() : base(new OsuStoryboardDecoder()) {}

        public StoryboardEditor(string path) : base(new OsuStoryboardDecoder(), path) {}
    }
}
