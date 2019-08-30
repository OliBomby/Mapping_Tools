using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class Editor {
        public string Path { get; set; }
        public ITextFile TextFile { get; set; }

        public Editor() {

        }

        public Editor(List<string> lines) {
            TextFile = new Beatmap(lines);
        }

        public Editor(string path) {
            Path = path;
            if (System.IO.Path.GetExtension(path) == ".osb") {
                TextFile = new StoryBoard(ReadFile(path));
            } else {
                TextFile = new Beatmap(ReadFile(path));
            }
        }

        public List<string> ReadFile(string path) {
            // Get contents of the file
            var lines = File.ReadAllLines(path);
            return new List<string>(lines);
        }

        public void SaveFile(string path) {
            SaveFile(path, TextFile.GetLines());
        }

        public void SaveFile(List<string> lines) {
            SaveFile(Path, lines);
        }

        public virtual void SaveFile() {
            SaveFile(Path, TextFile.GetLines());
        }

        public static void SaveFile(string path, List<string> lines) {
            if (!File.Exists(path)) {
                File.Create(path).Dispose();
            }

            File.WriteAllLines(path, lines);
        }

        public string GetBeatmapFolder() {
            return Directory.GetParent(Path).FullName;
        }

        public static string GetBeatmapFolder(string path)
        {
            return Directory.GetParent(path).FullName;
        }
    }
}
