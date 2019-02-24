using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Mapping_Tools.Classes.BeatmapHelper {
    class Editor {
        string Path { get; set; }
        public Beatmap Beatmap { get; set; }

        public Editor(List<string> lines) {
            Beatmap = new Beatmap(lines);
        }

        public Editor(string path) {
            Path = path;
            Beatmap = new Beatmap(ReadFile(Path));
        }

        public List<string> ReadFile(string path) {
            // Get contents of the file
            string[] linesz = new string[0];
            try {
                linesz = System.IO.File.ReadAllLines(path);
            }
            catch(Exception ex) {
                MessageBox.Show(ex.Message + "ABC");
            }
            var lines = new List<string>(linesz);
            return lines;
        }

        public List<HitObject> GetBookmarkedObjects() {
            return GetBookmarkedObjects(Beatmap);
        }

        public List<HitObject> GetBookmarkedObjects(Beatmap beatmap)
        {
            List<HitObject> markedObjects = new List<HitObject>();
            List<double> bookmarks = beatmap.GetBookmarks();
            foreach (HitObject ho in beatmap.HitObjects)
            {
                if (!bookmarks.Exists(o => (ho.Time <= o && o <= ho.EndTime))) { continue; }
                markedObjects.Add(ho);
            }
            return markedObjects;
        }

        public void SaveFile(string path, List<string> lines) {
            System.IO.File.WriteAllLines(path, lines);
        }

        public void SaveFile(string path) {
            System.IO.File.WriteAllLines(path, Beatmap.GetLines());
        }

        public void SaveFile(List<string> lines) {
            System.IO.File.WriteAllLines(Path, lines);
        }

        public void SaveFile() {
            System.IO.File.WriteAllLines(Path, Beatmap.GetLines());
        }
    }
}
