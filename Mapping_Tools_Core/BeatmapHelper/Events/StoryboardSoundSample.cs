using System;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Events {
    /// <summary>
    /// This represents a storyboarded sound sample for osu! storyboards. These can always be found under the [Events] -> (Storyboard Sound Samples) section.
    /// </summary>
    /// <example>
    /// Sample,56056,0,"soft-hitnormal.wav",30
    /// </example>
    public class StoryboardSoundSample : Event, IEquatable<StoryboardSoundSample>, IHasStartTime, IComparable<StoryboardSoundSample> {
        /// <summary>
        /// The time when this sound event occurs.
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// The storyboard layer this event belongs to.
        /// </summary>
        public StoryboardLayer Layer { get; set; }

        /// <summary>
        /// The name of the sample file which is the sound of this storyboard sample.
        /// This is a partial path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The volume of this sound. Ranges from 0 to 100.
        /// </summary>
        public double Volume { get; set; }

        public StoryboardSoundSample() { }

        /// <inheritdoc />
        public StoryboardSoundSample(int startTime, StoryboardLayer layer, string filePath, double volume) {
            StartTime = startTime;
            Layer = layer;
            FilePath = filePath;
            Volume = volume;
        }

        /// <inheritdoc />
        public StoryboardSoundSample(string line) {
            SetLine(line);
        }

        /// <summary>
        /// Serializes this object to .osu code.
        /// </summary>
        /// <returns></returns>
        public override string GetLine() {
            return $"Sample,{StartTime.ToInvariant()},{Layer.ToIntInvariant()},\"{FilePath}\",{Volume.ToRoundInvariant()}";
        }

        /// <summary>
        /// Deserializes a string of .osu code and populates the properties of this object.
        /// </summary>
        /// <param name="line"></param>
        public sealed override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Sample" && values[0] != "5") {
                throw new BeatmapParsingException("This line is not a storyboarded sample.", line);
            }

            if (InputParsers.TryParseDouble(values[1], out double t))
                StartTime = t;
            else throw new BeatmapParsingException("Failed to parse time of storyboarded sample.", line);

            if (Enum.TryParse(values[2], out StoryboardLayer layer))
                Layer = layer;
            else throw new BeatmapParsingException("Failed to parse layer of storyboarded sample.", line);

            FilePath = values[3].Trim('"');

            if (values.Length > 4) {
                if (InputParsers.TryParseDouble(values[4], out double vol))
                    Volume = vol;
                else throw new BeatmapParsingException("Failed to parse volume of storyboarded sample.", line);
            }
            else
                Volume = 100;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(StoryboardSoundSample other) {
            return
                other != null && (StartTime == other.StartTime &&
                                  Layer == other.Layer &&
                                  FilePath == other.FilePath &&
                                  Volume == other.Volume);
        }

        public int CompareTo(StoryboardSoundSample other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return StartTime.CompareTo(other.StartTime);
        }
    }
}
