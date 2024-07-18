﻿using System;
using System.Text;
using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
    /// </summary>
    public class ParameterCommand : Command, IHasEndTime {
        public override EventType EventType => EventType.P;
        public EasingType Easing { get; set; }
        public double EndTime { get; set; }
        public string Parameter { get; set; }

        public override string GetLine() {
            var builder = new StringBuilder(9);

            builder.Append(EventType.ToString());
            builder.Append(',');
            builder.Append(((int) Easing).ToInvariant());
            builder.Append(',');
            builder.Append(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant());
            builder.Append(',');
            if (!Precision.AlmostEquals(StartTime, EndTime)) {
                builder.Append(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant());
            }

            builder.Append(',');
            builder.Append(Parameter);

            return builder.ToString();
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            if (Enum.TryParse(values[1], out EasingType easingType))
                Easing = easingType;
            else throw new BeatmapParsingException("Failed to parse easing of command.", line);

            if (TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of param command.", line);

            // Set end time to start time if empty. This accounts for the shorthand
            if (string.IsNullOrEmpty(values[3])) {
                EndTime = StartTime;
            }
            else {
                if (TryParseDouble(values[3], out double endTime))
                    EndTime = endTime;
                else throw new BeatmapParsingException("Failed to parse end time of param command.", line);
            }

            Parameter = values[4];
        }
    }
}