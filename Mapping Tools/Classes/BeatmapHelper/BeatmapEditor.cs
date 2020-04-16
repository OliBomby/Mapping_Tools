using Mapping_Tools.Classes.Tools;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools.Classes.BeatmapHelper
{
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

        public override void SaveFile() {
            GenerateCoolSaveMD5(TextFile.GetLines());
            base.SaveFile();
        }

        public override void SaveFile(string path) {
            GenerateCoolSaveMD5(TextFile.GetLines());
            base.SaveFile(path);
        }

        public override void SaveFile(List<string> lines) {
            GenerateCoolSaveMD5(lines);
            base.SaveFile(lines);
        }

        private static void GenerateCoolSaveMD5(List<string> lines) {
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
