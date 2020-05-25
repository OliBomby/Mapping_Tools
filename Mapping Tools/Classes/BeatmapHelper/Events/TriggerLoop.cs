using System;
using System.Linq;
using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents trigger loop events. Although called loops, these only ever activate once.
    /// </summary>
    public class TriggerLoop : Event {
        public int Indents { get; set; }
        public EventType Event => EventType.T;
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public string TriggerName { get; set; }

        public override string GetLine() {
            var builder = new StringBuilder(8);

            builder.Append(new string(' ', Indents));
            builder.Append(Event.ToString());
            builder.Append(',');
            builder.Append(TriggerName);
            builder.Append(',');
            builder.Append(StartTime.ToRoundInvariant());
            builder.Append(',');
            builder.Append(EndTime.ToRoundInvariant());

            return builder.ToString();
        }

        public override void SetLine(string line) {
            int indents = line.TakeWhile(char.IsWhiteSpace).Count();
            Indents = indents;

            var subLine = line.Substring(indents);
            var values = subLine.Split(',');

            TriggerName = values[1];

            if (TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

            if (TryParseDouble(values[3], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
        }
    }
}