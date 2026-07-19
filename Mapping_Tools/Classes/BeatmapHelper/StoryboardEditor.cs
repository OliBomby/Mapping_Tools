using System.Collections.Generic;
using Mapping_Tools.ApplicationServices.Abstractions;

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

        public StoryboardEditor(List<string> lines, ITextFileStore fileStore) : base(fileStore) {
            TextFile = new StoryBoard(lines);
        }

        /// <inheritdoc />
        public StoryboardEditor(string path) {
            Path = path;
            TextFile = new StoryBoard(ReadFile(Path));
        }

        public StoryboardEditor(string path, ITextFileStore fileStore) : base(fileStore) {
            Path = path;
            TextFile = new StoryBoard(ReadFile(Path));
        }
    }
}
