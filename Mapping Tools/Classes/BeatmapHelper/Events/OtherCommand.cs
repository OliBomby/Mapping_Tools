using System;
using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents all the commands
    /// The exceptions being loops and triggers because these have different syntax.
    /// </summary>
    public class OtherCommand : Command, IHasEndTime {
        public EasingType Easing { get; set; }
        public int EndTime { get; set; }

        /// <summary>
        /// All other parameters
        /// </summary>
        public double[] Params { get; set; }

        public override string GetLine() {
            var builder = new StringBuilder(8 + Params.Length * 2);

            builder.Append(EventType.ToString());
            builder.Append(',');
            builder.Append(((int) Easing).ToInvariant());
            builder.Append(',');
            builder.Append(StartTime.ToInvariant());
            builder.Append(',');
            builder.Append(EndTime.ToInvariant());

            foreach (var param in Params) {
                builder.Append(',');
                builder.Append(param.ToInvariant());
            }

            return builder.ToString();
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            if (Enum.TryParse(values[0], out EventType eventType))
                EventType = eventType;
            else throw new BeatmapParsingException("Failed to parse type of command.", line);

            if (Enum.TryParse(values[1], out EasingType easingType))
                Easing = easingType;
            else throw new BeatmapParsingException("Failed to parse easing of command.", line);

            if (TryParseInt(values[2], out int startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of command.", line);

            // Set end time to start time if empty. This accounts for the shorthand
            if (string.IsNullOrEmpty(values[3])) {
                EndTime = StartTime;
            }
            else {
                if (TryParseInt(values[3], out int endTime))
                    EndTime = endTime;
                else throw new BeatmapParsingException("Failed to parse end time of command.", line);
            }

            Params = new double[values.Length - 4];
            for (int i = 4; i < values.Length; i++) {
                var stringValue = values[i];
                int index = i - 4;

                if (TryParseDouble(stringValue, out double value))
                    Params[index] = value;
                else throw new BeatmapParsingException($"Failed to parse value at position {i} of command.", line);
            }
        }
    }
}