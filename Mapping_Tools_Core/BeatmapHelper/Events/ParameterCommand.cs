
namespace Mapping_Tools_Core.BeatmapHelper.Events {
    /// <summary>
    /// Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
    /// </summary>
    public class ParameterCommand : Command, IHasEndTime {
        public override EventType EventType => EventType.P;
        public EasingType Easing { get; set; }
        public int EndTime { get; set; }
        public string Parameter { get; set; }

        public override string GetLine() {
            return $"{EventType},{((int)Easing).ToInvariant()},{StartTime.ToInvariant()},{EndTime.ToInvariant()},{Parameter}";
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            if (InputParsers.TryParseInt(values[1], out int startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of param command.", line);

            if (InputParsers.TryParseInt(values[2], out int endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of param command.", line);

            Parameter = values[3];
        }
    }
}