using System.Collections.Generic;
using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// WPF compatibility wrapper that uses the local filesystem by default.
    /// </summary>
    public class StoryboardEditor : StoryboardEditor2 {
        public StoryboardEditor(List<string> lines) : base(lines, LegacyFileStore.Default) { }

        public StoryboardEditor(List<string> lines, ITextFileStore fileStore) : base(lines, fileStore) { }

        public StoryboardEditor(string path) : base(path, LegacyFileStore.Default) { }

        public StoryboardEditor(string path, ITextFileStore fileStore) : base(path, fileStore) { }
    }
}
