using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class StoryboardSoundSample : IEquatable<StoryboardSoundSample>, ITextLine {
        public double Time { get; set; }
        public StoryboardLayer Layer { get; set; }
        public string FilePath { get; set; }
        public double Volume { get; set; }

        // Sample,56056,0,"soft-hitnormal.wav",30
        public StoryboardSoundSample(double time, StoryboardLayer layer, string filePath, double volume) {
            Time = time;
            Layer = layer;
            FilePath = filePath;
            Volume = volume;
        }

        public StoryboardSoundSample(string line) {
            SetLine(line);
        }

        public string GetLine() {
            return string.Format("Sample,{0},{1},\"{2}\",{3}", Math.Round(Time), Layer, FilePath, Math.Round(Volume));
        }

        public void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Sample") {
                throw new Exception("Can not parse this line because it's not a storyboarded sample.");
            }

            Time = ParseDouble(values[1]);
            Layer = (StoryboardLayer)int.Parse(values[2]);
            FilePath = values[3].Trim('"');
            Volume = values.Length >= 5 && values[4] != "" ? ParseDouble(values[4]) : 100;
        }

        private double ParseDouble(string d) {
            return double.Parse(d, CultureInfo.InvariantCulture);
        }

        public bool Equals(StoryboardSoundSample other) {
            return
                Time == other.Time &&
                Layer == other.Layer &&
                FilePath == other.FilePath &&
                Volume == other.Volume;
        }
    }
}
