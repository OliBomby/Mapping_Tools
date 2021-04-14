using System;
using System.Text;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Events {
    /// <summary>
    /// Represents all the commands
    /// The exceptions being loops and triggers because these have different syntax.
    /// </summary>
    public class OtherCommand : Command, IHasDuration {
        public EasingType Easing { get; set; }
        public double Duration => EndTime - StartTime;
        public double EndTime { get; set; }

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
            builder.Append(StartTime.ToRoundInvariant());
            builder.Append(',');
            builder.Append(EndTime.ToRoundInvariant());

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

            if (InputParsers.TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of command.", line);

            // Set end time to start time if empty. This accounts for the shorthand
            if (string.IsNullOrEmpty(values[3])) {
                EndTime = StartTime;
            }
            else {
                if (InputParsers.TryParseDouble(values[3], out double endTime))
                    EndTime = endTime;
                else throw new BeatmapParsingException("Failed to parse end time of command.", line);
            }

            Params = new double[values.Length - 4];
            for (int i = 4; i < values.Length; i++) {
                var stringValue = values[i];
                int index = i - 4;

                if (InputParsers.TryParseDouble(stringValue, out double value))
                    Params[index] = value;
                else throw new BeatmapParsingException($"Failed to parse value at position {i} of command.", line);
            }
        }
    }
}