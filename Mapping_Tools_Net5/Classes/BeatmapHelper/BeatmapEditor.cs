using Mapping_Tools.Classes.Tools;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class BeatmapEditor : Editor
    {
        public Beatmap Beatmap => (Beatmap)TextFile;

        public BeatmapEditor(List<string> lines)
        {
            TextFile = new Beatmap(lines);
        }

        public BeatmapEditor(string path)
        {
            Path = path;
            TextFile = new Beatmap(ReadFile(Path));
        }

        /// <summary>
        /// Saves the beatmap just like <see cref="SaveFile()"/> but also updates the filename according to the metadata of the <see cref="Beatmap"/>
        /// </summary>
        /// <remarks>This method also updates the Path property</remarks>
        public void SaveFileWithNameUpdate() {
            // Remove the beatmap with the old filename
            File.Delete(Path);

            // Save beatmap with the new filename
            Path = System.IO.Path.Combine(GetParentFolder(), Beatmap.GetFileName());
            SaveFile();
        }

        public override void SaveFile() {
            GenerateBetterSaveMD5(TextFile.GetLines());
            base.SaveFile();
        }

        public override void SaveFile(string path) {
            GenerateBetterSaveMD5(TextFile.GetLines());
            base.SaveFile(path);
        }

        public override void SaveFile(List<string> lines) {
            GenerateBetterSaveMD5(lines);
            base.SaveFile(lines);
        }

        private static void GenerateBetterSaveMD5(List<string> lines) {
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
