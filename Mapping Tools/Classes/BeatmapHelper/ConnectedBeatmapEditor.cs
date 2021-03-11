using System;
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

        /// <summary>
        /// Doesn't garantee actual newest version.
        /// If you want an exception thrown when it fails to read the editor use <see cref="ReadFileUnsafe"/>.
        /// </summary>
        /// <returns></returns>
        public override Beatmap ReadFile() {
            Beatmap beatmap;
            try {
                beatmap = ReadFileUnsafe();
            } catch (Exception e) {
                Console.WriteLine(e);
                beatmap = base.ReadFile();
            }

            return beatmap;
        }

        /// <summary>
        /// Version of ReadFile with exception if newest version couldn't not be fetched.
        /// </summary>
        /// <returns>The parsed beatmap</returns>
        public Beatmap ReadFileUnsafe() {
            var beatmap = base.ReadFile();

            EditorReaderStuff.UpdateBeatmap(beatmap, Path, reader ?? EditorReaderStuff.GetFullEditorReader());

            return beatmap;
        }
    }
}