
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Events {
    /// <summary>
    /// Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
    /// </summary>
    public class ParameterCommand : Command, IHasDuration {
        public override EventType EventType => EventType.P;
        public EasingType Easing { get; set; }
        public double Duration => EndTime - StartTime;
        public double EndTime { get; set; }
        public string Parameter { get; set; }

        public override string GetLine() {
            return $"{EventType},{((int)Easing).ToInvariant()},{StartTime.ToRoundInvariant()},{EndTime.ToRoundInvariant()},{Parameter}";
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            if (InputParsers.TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of param command.", line);

            if (InputParsers.TryParseDouble(values[2], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of param command.", line);

            Parameter = values[3];
        }
    }
}