using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class HitsoundLayer : INotifyPropertyChanged {
        private string name;
        public string Name {
            get { return name; }
            set {
                if (name != value) {
                    name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private double x;
        public double X {
            get { return x; }
            set {
                if (x != value) {
                    x = value;
                    NotifyPropertyChanged("X");
                }
            }
        }

        private double y;
        public double Y {
            get { return y; }
            set {
                if (y != value) {
                    y = value;
                    NotifyPropertyChanged("Y");
                }
            }
        }

        private int sampleSet;
        public int SampleSet {
            get { return sampleSet; }
            set {
                if (sampleSet != value) {
                    sampleSet = value;
                    NotifyPropertyChanged("SampleSet");
                }
            }
        }

        private int hitsound;
        public int Hitsound {
            get { return hitsound; }
            set {
                if (hitsound != value) {
                    hitsound = value;
                    NotifyPropertyChanged("Hitsound");
                }
            }
        }
        
        public List<double> Times { get; set; }

        private int priority;
        public int Priority {
            get { return priority; }
            set {
                if (priority != value) {
                    priority = value;
                    NotifyPropertyChanged("Priority");
                }
            }
        }

        private string path;
        public string Path {
            get { return path; }
            set {
                if (path != value) {
                    path = value;
                    NotifyPropertyChanged("Path");
                }
            }
        }

        private string samplePath;
        public string SamplePath {
            get { return samplePath; }
            set {
                if (samplePath != value) {
                    samplePath = value;
                    NotifyPropertyChanged("SamplePath");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public string SampleSetString { get => GetSampleSetString(); set => SetSampleSetString(value); }

        private void SetSampleSetString(string value) {
            if (value == "Auto") { SampleSet = 0; }
            else if (value == "Normal") { SampleSet = 1; }
            else if (value == "Soft") { SampleSet = 2; }
            else if (value == "Drum") { SampleSet = 3; }
            else { SampleSet = 4; }
            NotifyPropertyChanged("SampleSetString");
        }

        private string GetSampleSetString() {
            if (SampleSet == 0) { return "Auto"; }
            else if (SampleSet == 1) { return "Normal"; }
            else if (SampleSet == 2) { return "Soft"; }
            else if (SampleSet == 3) { return "Drum"; }
            else { return "None"; }
        }

        public string HitsoundString { get => GetHitsoundString(); set => SetHitsoundString(value); }

        private void SetHitsoundString(string value) {
            if (value == "Normal") { Hitsound = 0; }
            else if (value == "Whistle") { Hitsound = 1; }
            else if (value == "Finish") { Hitsound = 2; }
            else if (value == "Clap") { Hitsound = 3; }
            else { Hitsound = 4; }
            NotifyPropertyChanged("HitsoundString");
        }

        private string GetHitsoundString() {
            if (Hitsound == 0) { return "Normal"; }
            else if (Hitsound == 1) { return "Whistle"; }
            else if (Hitsound == 2) { return "Finish"; }
            else if (Hitsound == 3) { return "Clap"; }
            else { return "None"; }
        }

        public int SampleSetComboBoxIndex { get => GetSampleSetComboBoxIndex(); set => SetSampleSetComboBoxIndex(value); }

        private void SetSampleSetComboBoxIndex(int value) {
            SampleSet = value + 1;
        }

        private int GetSampleSetComboBoxIndex() {
            return SampleSet - 1;
        }

        public HitsoundLayer() {
            Name = "";
            Path = "";
            X = -1;
            Y = -1;
            Times = new List<double>();
        }

        public HitsoundLayer(string name, string path, double x, double y) {
            Name = name;
            Path = path;
            X = x;
            Y = y;
            ImportMap(path, x, y);
        }

        public HitsoundLayer(string name, string path, double x, double y, int priority) {
            Name = name;
            Path = path;
            X = x;
            Y = y;
            Priority = priority;
            ImportMap(path, x, y);
        }

        public HitsoundLayer(string name, string path, double x, double y, int sampleSet, int hitsound, string samplePath) {
            Name = name;
            Path = path;
            X = x;
            Y = y;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            ImportMap(path, x, y);
        }

        public HitsoundLayer(string name, string path, double x, double y, int sampleSet, int hitsound, string samplePath, int priority) {
            Name = name;
            Path = path;
            X = x;
            Y = y;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
            ImportMap(path, x, y);
        }

        public void SetPriority(int priority) {
            Priority = priority;
        }

        public void ImportMap() {
            Times = new List<double>();
            Editor editor = new Editor(Path);

            bool xIgnore = X == -1;
            bool yIgnore = Y == -1;

            foreach (HitObject ho in editor.Beatmap.HitObjects) {
                if ((Math.Abs(ho.Pos.X - X) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - Y) < 3 || yIgnore)) {
                    Times.Add(ho.Time);
                }
            }
            NotifyPropertyChanged("Times");
        }

        public void ImportMap(string path, double x, double y) {
            Times = new List<double>();
            Editor editor = new Editor(path);

            bool xIgnore = x == -1;
            bool yIgnore = y == -1;

            foreach (HitObject ho in editor.Beatmap.HitObjects) {
                if ((Math.Abs(ho.Pos.X - x) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - y) < 3 || yIgnore)) {
                    Times.Add(ho.Time);
                }
            }
            NotifyPropertyChanged("Times");
        }
    }
}
