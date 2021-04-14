
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Events {
    /// <summary>
    /// Represents trigger loop events. Although called loops, these only ever activate once.
    /// </summary>
    public class TriggerLoop : Command, IHasDuration {
        public override EventType EventType => EventType.T;
        public double Duration => EndTime - StartTime;
        public double EndTime { get; set; }
        public string TriggerName { get; set; }

        public override string GetLine() {
            return $"{EventType},{TriggerName},{StartTime.ToRoundInvariant()},{EndTime.ToRoundInvariant()}";
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            TriggerName = values[1];

            if (InputParsers.TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

            if (InputParsers.TryParseDouble(values[3], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
        }
    }
}