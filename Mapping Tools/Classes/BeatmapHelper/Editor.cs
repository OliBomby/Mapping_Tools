using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Classes.BeatmapHelper {
    class Editor {
        string BeatmapPath { get; set; }
        public Beatmap Beatmap { get; set; }

        public Editor(List<string> lines) {
            Beatmap = new Beatmap(lines);
        }

        public Editor(string path) {
            BeatmapPath = path;
            Beatmap = new Beatmap(ReadFile(BeatmapPath));
        }

        public List<string> ReadFile(string path) {
            // Get contents of the file
            string[] linesz = new string[0];
            try {
                linesz = File.ReadAllLines(path);
            }
            catch(Exception ex) {
                MessageBox.Show(ex.Message);
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

        public void SaveFile(string path) {
            SaveFile(path, Beatmap.GetLines());
        }

        public void SaveFile(List<string> lines) {
            SaveFile(BeatmapPath, lines);
        }

        public void SaveFile() {
            SaveFile(BeatmapPath, Beatmap.GetLines());
        }

        public static void SaveFile(string path, List<string> lines) {
            if (!File.Exists(path)) {
                File.Create(path).Dispose();
            }

            File.WriteAllLines(path, lines);
        }
    }
}
