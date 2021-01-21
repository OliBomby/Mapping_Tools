using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Beatmap editor for use with Editor Reader.
    /// </summary>
    public class HashingBeatmapEditor : BeatmapEditor {
        public HashingBeatmapEditor() {}

        public HashingBeatmapEditor(string path) : base(path) {}

        public override void WriteFile(Beatmap beatmap) {
            var lines = parser.Serialize(beatmap).ToList();
            GenerateBetterSaveMD5(lines);
            base.SaveFile(lines);
        }

        private static void GenerateBetterSaveMD5(IEnumerable<string> lines) {
            var tempPath = System.IO.Path.Combine(MainWindow.AppDataPath, "temp.osu");

            if (!File.Exists(tempPath))
            {
                File.Create(tempPath).Dispose();
            }
            File.WriteAllLines(tempPath, lines);

            EditorReaderStuff.DontCoolSaveWhenMD5EqualsThisString = EditorReaderStuff.GetMD5FromPath(tempPath);
        }
    }
}
