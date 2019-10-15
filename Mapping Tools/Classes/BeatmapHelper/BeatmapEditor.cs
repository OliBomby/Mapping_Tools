using Mapping_Tools.Classes.Tools;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools.Classes.BeatmapHelper {

    /// <summary>
    /// 
    /// </summary>
    public class BeatmapEditor : Editor {

        /// <summary>
        /// 
        /// </summary>
        public Beatmap Beatmap => (Beatmap)TextFile;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        public BeatmapEditor(List<string> lines) {
            TextFile = new Beatmap(lines);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public BeatmapEditor(string path) {
            Path = path;
            TextFile = new Beatmap(ReadFile(Path));
        }
        
        /// <summary>
        /// Saves the current file within a temporary file before using CoolSave
        /// </summary>
        public override void SaveFile() {
            var tempPath = System.IO.Path.Combine(MainWindow.AppDataPath, "temp.osu");

            if (!File.Exists(tempPath)) {
                File.Create(tempPath).Dispose();
            }
            File.WriteAllLines(tempPath, TextFile.GetLines());

            EditorReaderStuff.Md5ComparasonString = EditorReaderStuff.GetMD5FromPath(tempPath);

            base.SaveFile();
        }
    }
}
