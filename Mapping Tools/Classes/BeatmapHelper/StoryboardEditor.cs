using System.Collections.Generic;

namespace Mapping_Tools.Classes.BeatmapHelper
{
    public class StoryboardEditor : Editor
    {
        public StoryBoard StoryBoard { get => (StoryBoard)TextFile; }

        public StoryboardEditor(List<string> lines) {
            TextFile = new StoryBoard(lines);
        }

        public StoryboardEditor(string path) {
            Path = path;
            TextFile = new StoryBoard(ReadFile(Path));
        }
    }
}
