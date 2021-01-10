using Editor_Reader;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Beatmap editor that tries to get the newest data from osu! memory.
    /// </summary>
    public class ConnectedBeatmapEditor : HashingBeatmapEditor {
        private readonly EditorReader reader;

        public ConnectedBeatmapEditor() { }

        public ConnectedBeatmapEditor(string path) : base(path) { }

        public ConnectedBeatmapEditor(string path, EditorReader reader) : base(path) {
            this.reader = reader;
        }

        public override void ReadFile() {
            base.ReadFile();

            if (reader != null) {
                EditorReaderStuff.UpdateEditorOrNot(this, reader, out _, out _);
            } else {
                EditorReaderStuff.UpdateEditorOrNot(this, out _, out _);
            }
        }
    }
}