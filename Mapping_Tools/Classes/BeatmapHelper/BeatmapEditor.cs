using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.SystemTools;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// WPF compatibility wrapper that adds editor-reader MD5 coordination.
    /// </summary>
    public class BeatmapEditor : BeatmapEditor2 {
        public BeatmapEditor(List<string> lines) : base(lines, LegacyFileStore.Default) { }

        public BeatmapEditor(List<string> lines, ITextFileStore fileStore) : base(lines, fileStore) { }

        public BeatmapEditor(string path) : base(path, LegacyFileStore.Default) { }

        public BeatmapEditor(string path, ITextFileStore fileStore) : base(path, fileStore) { }

        protected override void BeforeSave(List<string> lines) {
            var tempPath = System.IO.Path.Combine(SettingsManager.ApplicationDataPath, "temp.osu");

            if (!File.Exists(tempPath)) {
                File.Create(tempPath).Dispose();
            }

            File.WriteAllLines(tempPath, lines);
            EditorReaderStuff.DontCoolSaveWhenMd5EqualsThisString = EditorReaderStuff.GetMd5FromPath(tempPath);
        }
    }
}
