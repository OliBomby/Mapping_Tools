using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Beatmap editor for use with Editor Reader.
    /// </summary>
    public class ConnectedBeatmapEditor : BeatmapEditor {
        public ConnectedBeatmapEditor() {}

        public ConnectedBeatmapEditor(string path) : base(path) {
            // TODO: Get newest version here
        }

        public override void SaveFile() {
            GenerateBetterSaveMD5(parser.Serialize(Instance));
            base.SaveFile();
        }

        public override void SaveFile(string path) {
            GenerateBetterSaveMD5(parser.Serialize(Instance));
            base.SaveFile(path);
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
