using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class HitsoundLayer {
        public string Name { get; set; }
        public string Path { get; set; }
        public Vector2 Pos { get; set; }
        public int SampleSet { get; set; }
        public int Hitsound { get; set; }
        public string SamplePath { get; set; }
        public List<double> Times { get; set; }
        public int Priority { get; set; }

        public string SampleSetString { get => GetSampleSetString(); }

        private string GetSampleSetString() {
            if (SampleSet == 0) { return "Auto"; }
            else if (SampleSet == 1) { return "Normal"; }
            else if (SampleSet == 2) { return "Soft"; }
            else if (SampleSet == 3) { return "Drum"; }
            else { return "None"; }
        }

        public string HitsoundString { get => GetHitsoundString(); }

        private string GetHitsoundString() {
            if (Hitsound == 0) { return "Normal"; }
            else if (Hitsound == 1) { return "Whistle"; }
            else if (Hitsound == 2) { return "Finish"; }
            else if (Hitsound == 3) { return "Clap"; }
            else { return "None"; }
        }

        public HitsoundLayer(string name, string path, Vector2 pos) {
            Name = name;
            Path = path;
            Pos = pos;
            ImportMap(path, pos);
        }

        public HitsoundLayer(string name, string path, Vector2 pos, int priority) {
            Name = name;
            Path = path;
            Pos = pos;
            Priority = priority;
            ImportMap(path, pos);
        }

        public HitsoundLayer(string name, string path, Vector2 pos, int sampleSet, int hitsound, string samplePath) {
            Name = name;
            Path = path;
            Pos = pos;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            ImportMap(path, pos);
        }

        public HitsoundLayer(string name, string path, Vector2 pos, int sampleSet, int hitsound, string samplePath, int priority) {
            Name = name;
            Path = path;
            Pos = pos;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
            ImportMap(path, pos);
        }

        public void SetPriority(int priority) {
            Priority = priority;
        }

        public void ImportMap(string path, Vector2 pos) {
            Times = new List<double>();
            Editor editor = new Editor(path);

            bool xIgnore = pos.X == -1;
            bool yIgnore = pos.Y == -1;

            foreach (HitObject ho in editor.Beatmap.HitObjects) {
                if ((Math.Abs(ho.Pos.X - pos.X) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - pos.Y) < 3 || yIgnore)) {
                    Times.Add(ho.Time);
                }
            }
        }
    }
}
