using System;
using System.Linq;
using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents the standard loop event. This event has a different syntax so it can't be a <see cref="Param"/>.
    /// </summary>
    public class StandardLoop : Event {
        public int Indents { get; set; }
        public EventType Event => EventType.L;
        public double StartTime { get; set; }

        public int LoopCount { get; set; }

        public override string GetLine() {
            var builder = new StringBuilder(6);

            builder.Append(new string(' ', Indents));
            builder.Append(Event.ToString());
            builder.Append(',');
            builder.Append(StartTime.ToRoundInvariant());
            builder.Append(',');
            builder.Append(LoopCount.ToInvariant());

            return builder.ToString();
        }

        public override void SetLine(string line) {
            int indents = line.TakeWhile(char.IsWhiteSpace).Count();
            Indents = indents;

            var subLine = line.Substring(indents);
            var values = subLine.Split(',');

            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

            if (TryParseInt(values[2], out int loopCount))
                LoopCount = loopCount;
            else throw new BeatmapParsingException("Failed to parse loop count of event param.", line);
        }
    }
}