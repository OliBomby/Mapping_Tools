using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class StoryboardSoundSample {
        public enum Layers {
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3
        }


        public double Time { get; set; }
        public int Layer { get; set; }
        public string FilePath { get; set; }
        public double Volume { get; set; }

        // Sample,56056,0,"soft-hitnormal.wav",30
        public StoryboardSoundSample(double time, int layer, string filePath, double volume) {
            Time = time;
            Layer = layer;
            FilePath = filePath;
            Volume = volume;
        }

        public StoryboardSoundSample(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Sample") {
                throw new Exception("Can not parse this line because it's not a storyboarded sample.");
            }

            Time = ParseDouble(values[1]);
            Layer = int.Parse(values[2]);
            FilePath = values[3].Trim('"');
            Volume = values.Length >= 5 ? ParseDouble(values[4]) : 100;
        }

        private double ParseDouble(string d) {
            return double.Parse(d, CultureInfo.InvariantCulture);
        }

        public string GetLine() {
            return String.Format("Sample,{0},{1},\"{2}\",{3}", Math.Round(Time), Layer, FilePath, Math.Round(Volume));
        }
    }
}
