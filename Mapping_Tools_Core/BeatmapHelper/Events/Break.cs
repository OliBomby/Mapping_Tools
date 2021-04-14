
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Events {
    public class Break : Event, IHasStartTime, IHasDuration {
        public string EventType { get; set; }
        public double StartTime { get; set; }
        public double Duration => EndTime - StartTime;
        public double EndTime { get; set; }

        public Break() { }

        public Break(string line) {
            SetLine(line);
        }

        public override string GetLine() {
            return $"{EventType},{StartTime.ToRoundInvariant()},{EndTime.ToRoundInvariant()}";
        }

        public sealed override void SetLine(string line) {
            string[] values = line.Split(',');

            // Either 'Break' or '2' indicates a break. We save the value so we dont accidentally change it.
            if (values[0] != "2" && values[0] != "Break") {
                throw new BeatmapParsingException("This line is not a break.", line);
            }

            EventType = values[0];

            if (InputParsers.TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of break.", line);

            if (InputParsers.TryParseDouble(values[2], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of break.", line);
        }
    }
}