using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Break : Event, IHasStartTime, IHasEndTime {
        public string EventType { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }

        public Break() { }

        public Break(string line) {
            SetLine(line);
        }

        public override string GetLine() {
            return $"{EventType},{StartTime.ToInvariant()},{EndTime.ToInvariant()}";
        }

        public sealed override void SetLine(string line) {
            string[] values = line.Split(',');

            // Either 'Break' or '2' indicates a break. We save the value so we dont accidentally change it.
            if (values[0] != "2" && values[0] != "Break") {
                throw new BeatmapParsingException("This line is not a break.", line);
            }

            EventType = values[0];

            // This start time is usually 0 for backgrounds but lets parse it anyways
            if (TryParseInt(values[1], out int startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of break.", line);

            // This start time is usually 0 for backgrounds but lets parse it anyways
            if (TryParseInt(values[1], out int endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse start time of break.", line);
        }
    }
}