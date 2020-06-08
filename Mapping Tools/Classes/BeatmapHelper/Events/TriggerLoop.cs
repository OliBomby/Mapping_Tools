using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents trigger loop events. Although called loops, these only ever activate once.
    /// </summary>
    public class TriggerLoop : Command, IHasEndTime {
        public override EventType EventType => EventType.T;
        public int EndTime { get; set; }
        public string TriggerName { get; set; }

        public override string GetLine() {
            return $"{EventType},{TriggerName},{StartTime.ToInvariant()},{EndTime.ToInvariant()}";
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            TriggerName = values[1];

            if (TryParseInt(values[2], out int startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

            if (TryParseInt(values[3], out int endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
        }
    }
}