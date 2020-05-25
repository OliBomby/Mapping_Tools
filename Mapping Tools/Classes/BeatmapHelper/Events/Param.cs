using System;
using System.Linq;
using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents all the events that can be placed underneath another event except loops and triggers.
    /// </summary>
    public class Param : Event {
        public int Indents { get; set; }
        public EventType Event { get; set; }
        public EasingType Easing { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        /// <summary>
        /// All other parameters
        /// </summary>
        public double[] Params { get; set; }

        public override string GetLine() {
            var builder = new StringBuilder(5 + Params.Length);

            builder.Append(new string(' ', Indents));
            builder.Append(Event.ToString());
            builder.Append(((int) Easing).ToInvariant());
            builder.Append(StartTime.ToRoundInvariant());
            builder.Append(EndTime.ToRoundInvariant());

            foreach (var param in Params) {
                builder.Append(param.ToInvariant());
            }

            return builder.ToString();
        }

        public override void SetLine(string line) {
            int indents = line.TakeWhile(char.IsWhiteSpace).Count();
            Indents = indents;

            var subLine = line.Substring(indents);
            var values = subLine.Split(',');

            if (Enum.TryParse(values[0], out EventType eventType))
                Event = eventType;
            else throw new BeatmapParsingException("Failed to parse type of event param.", line);

            if (Enum.TryParse(values[1], out EasingType easingType))
                Easing = easingType;
            else throw new BeatmapParsingException("Failed to parse easing of event param.", line);

            if (TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

            // Set end time to start time if empty. This accounts for the shorthand
            if (string.IsNullOrEmpty(values[3])) {
                EndTime = StartTime;
            }
            else {
                if (TryParseDouble(values[3], out double endTime))
                    EndTime = endTime;
                else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
            }

            Params = new double[values.Length - 4];
            for (int i = 4; i < values.Length; i++) {
                var stringValue = values[i];
                int index = i - 4;

                if (TryParseDouble(stringValue, out double value))
                    Params[index] = value;
                else throw new BeatmapParsingException($"Failed to parse value at position {i} of event param.", line);
            }
        }
    }
}