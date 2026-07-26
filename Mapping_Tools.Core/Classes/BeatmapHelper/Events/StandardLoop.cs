using static Mapping_Tools.Core.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents the standard loop event. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
    /// </summary>
#nullable disable

    public class StandardLoop : Command {
        /// <summary>
        /// Gets the standard-loop command token.
        /// </summary>
        public override EventType EventType => EventType.L;

        /// <summary>
        /// Gets or sets how many times the nested command group repeats.
        /// </summary>
        public int LoopCount { get; set; }

        /// <summary>
        /// <inheritdoc/>
        public override string GetLine() {
            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{LoopCount.ToInvariant()}";
        }

        /// <summary>
        /// <inheritdoc/>
        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
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
