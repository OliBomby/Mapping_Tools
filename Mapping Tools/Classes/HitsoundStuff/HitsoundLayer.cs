
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        private string importType;
        public string ImportType {
            get { return importType; }
            set {
                if (importType != value) {
                    importType = value;
                    NotifyPropertyChanged("ImportType");
                    NotifyPropertyChanged("CoordinateVisibility");
                    NotifyPropertyChanged("KeysoundVisibility");
                }
            }
        }
        
        public Visibility CoordinateVisibility { get { if (ImportType == "Stack") { return Visibility.Visible; } else { return Visibility.Collapsed; } } }

        public Visibility KeysoundVisibility { get { if (ImportType == "MIDI") { return Visibility.Visible; } else { return Visibility.Collapsed; } } }

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

        private double instrument;
        public double Instrument {
            get { return instrument; }
            set {
                if (instrument != value) {
                    instrument = value;
                    NotifyPropertyChanged("Instrument");
                }
            }
        }

        private double note;
        public double Note {
            get { return note; }
            set {
                if (note != value) {
                    note = value;
                    NotifyPropertyChanged("Note");
                }
            }
        }

        private double length;
        public double Length {
            get { return length; }
            set {
                if (length != value) {
                    length = value;
                    NotifyPropertyChanged("Length");
                }
            }
        }

        private double velocity;
        public double Velocity {
            get { return velocity; }
            set {
                if (velocity != value) {
                    velocity = value;
                    NotifyPropertyChanged("Velocity");
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

        private List<double> times;
        public List<double> Times {
            get { return times; }
            set {
                if (times != value) {
                    times = value;
                    NotifyPropertyChanged("Times");
                }
            }
        }

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

        public HitsoundLayer(string name, string importType, string path, int sampleSet, int hitsound, string samplePath) {
            Name = name;
            Path = path;
            ImportType = importType;
            X = -1;
            Y = -1;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Times = new List<double>();
        }

        public HitsoundLayer(string name, string importType, string path, double x, double y) {
            Name = name;
            Path = path;
            ImportType = importType;
            X = x;
            Y = y;
            Times = new List<double>();
            Import();
        }

        public HitsoundLayer(string name, string importType, string path, double x, double y, int priority) {
            Name = name;
            Path = path;
            ImportType = importType;
            X = x;
            Y = y;
            Priority = priority;
            Times = new List<double>();
            Import();
        }

        public HitsoundLayer(string name, string importType, string path, double x, double y, int sampleSet, int hitsound, string samplePath) {
            Name = name;
            Path = path;
            ImportType = importType;
            X = x;
            Y = y;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Times = new List<double>();
            Import();
        }

        public HitsoundLayer(string name, string importType, string path, double x, double y, int sampleSet, int hitsound, string samplePath, int priority) {
            Name = name;
            Path = path;
            ImportType = importType;
            X = x;
            Y = y;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
            Times = new List<double>();
            Import();
        }

        public void SetPriority(int priority) {
            Priority = priority;
        }

        public void Import(List<HitsoundLayer> layers = null) {
            if (ImportType == "Stack") {
                ImportStack(Path, x, y);
            } else if (ImportType == "Hitsounds") {
                // Import complete hitsounds
                layers = layers ?? HitsoundImporter.LayersFromHitsounds(Path);
                
                HitsoundLayer sameLayer = layers.Find(o => o.Path == Path);
                if (sameLayer != null) {
                    Times = sameLayer.Times;
                }

            } else if (ImportType == "MIDI"){
                // Import MIDI
                layers = layers ?? HitsoundImporter.ImportMIDI(Path);

                List<HitsoundLayer> sameLayer = layers.FindAll(o => Instrument == o.Instrument && (Note == -1 || Note == o.Note) && (Length == -1 || Length == o.Length)
                                                                                               && (Velocity == -1 || Velocity == o.Velocity));
                Times.Clear();
                foreach (HitsoundLayer hsl in sameLayer) {
                    Times.AddRange(hsl.Times);
                }
                Times.OrderBy(o => o);
            }
            NotifyPropertyChanged("Times");
        }

        public void ImportStack(string path, double x, double y) {
            Times = HitsoundImporter.TimesFromStack(path, x, y);
        }
    }
}
