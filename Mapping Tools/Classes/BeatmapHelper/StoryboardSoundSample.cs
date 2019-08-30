using System;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

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
            return $"Sample,{Time.ToRoundInvariant()},{Layer.ToIntInvariant()},\"{FilePath}\",{Volume.ToRoundInvariant()}";
        }

        public void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Sample") {
                throw new BeatmapParsingException("This line is not a storyboarded sample.", line);
            }

            if (TryParseDouble(values[1], out double t))
                Time = t;
            else throw new BeatmapParsingException("Failed to parse time of storyboarded sample.", line);

            if (Enum.TryParse(values[2], out StoryboardLayer layer))
                Layer = layer;
            else throw new BeatmapParsingException("Failed to parse layer of storyboarded sample.", line);

            FilePath = values[3].Trim('"');

            if (values.Length > 4) {
                if (TryParseDouble(values[4], out double vol))
                    Volume = vol;
                else throw new BeatmapParsingException("Failed to parse volume of storyboarded sample.", line);
            }
            else
                Volume = 100;
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
