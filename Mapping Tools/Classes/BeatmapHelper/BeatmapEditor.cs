using Mapping_Tools.Classes.Tools;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools.Classes.BeatmapHelper
{
    public class BeatmapEditor : Editor
    {
        public Beatmap Beatmap { get => (Beatmap)TextFile; }

        public BeatmapEditor(List<string> lines)
        {
            TextFile = new Beatmap(lines);
        }

        public BeatmapEditor(string path)
        {
            Path = path;
            TextFile = new Beatmap(ReadFile(Path));
        }

        public override void SaveFile()
        {
            var tempPath = System.IO.Path.Combine(MainWindow.AppDataPath, "temp.osu");

            if (!File.Exists(tempPath))
            {
                File.Create(tempPath).Dispose();
            }
            File.WriteAllLines(tempPath, TextFile.GetLines());

            EditorReaderStuff.DontCoolSaveWhenMD5EqualsThisString = EditorReaderStuff.GetMD5FromPath(tempPath);

            base.SaveFile();
        }
    }
}
