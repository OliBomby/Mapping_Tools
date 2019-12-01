using System.Collections.Generic;

namespace Mapping_Tools.Classes.BeatmapHelper
{
    /// <summary>
    /// 
    /// </summary>
    public class StoryboardEditor : Editor
    {
        /// <summary>
        /// 
        /// </summary>
        public StoryBoard StoryBoard => (StoryBoard)TextFile;

        /// <inheritdoc />
        public StoryboardEditor(List<string> lines) {
            TextFile = new StoryBoard(lines);
        }

        /// <inheritdoc />
        public StoryboardEditor(string path) {
            Path = path;
            TextFile = new StoryBoard(ReadFile(Path));
        }
    }
}
