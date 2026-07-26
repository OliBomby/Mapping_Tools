using System.Collections.Generic;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// WPF compatibility wrapper that uses the local filesystem by default.
    /// </summary>
    public class Editor : Editor2 {
        public Editor() : base(LegacyFileStore.Default) { }

        public Editor(ITextFileStore fileStore) : base(fileStore) { }

        public Editor(List<string> lines) : base(lines, LegacyFileStore.Default) { }

        public Editor(List<string> lines, ITextFileStore fileStore) : base(lines, fileStore) { }

        public Editor(string path) : base(path, LegacyFileStore.Default) { }

        public Editor(string path, ITextFileStore fileStore) : base(path, fileStore) { }

        public static void SaveFile(string path, List<string> lines) {
            Editor2.SaveFile(LegacyFileStore.Default, path, lines);
        }

        public static string GetParentFolder(string path) {
            return Editor2.GetParentFolder(LegacyFileStore.Default, path);
        }
    }
}
