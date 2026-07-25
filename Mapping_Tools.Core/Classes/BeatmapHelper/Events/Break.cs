using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
#nullable disable

    /// <summary>
    /// Represents a gameplay break interval from the beatmap events section.
    /// </summary>
    public class Break : Event, IHasStartTime, IHasEndTime {
        /// <summary>
        /// Gets or sets the original break token, preserving <c>2</c> or <c>Break</c>.
        /// </summary>
        public string EventType { get; set; }
        /// <summary>
        /// <inheritdoc/>
        public double StartTime { get; set; }
        /// <summary>
        /// <inheritdoc/>
        public double EndTime { get; set; }

        /// <summary>
        /// Creates an uninitialized break event for property-based construction.
        /// </summary>
        public Break() { }

        /// <summary>
        /// Parses a break event from a serialized line.
        /// </summary>
        /// <param name="line">A <c>2</c> or <c>Break</c> event line.</param>
        public Break(string line) {
            SetLine(line);
        }

        /// <summary>
        /// <inheritdoc/>
        public override string GetLine() {
            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant())}";
        }

        /// <summary>
        /// <inheritdoc/>
        public override sealed void SetLine(string line) {
            string[] values = line.Split(',');

            // Either 'Break' or '2' indicates a break. We save the value so we dont accidentally change it.
            if (values[0] != "2" && values[0] != "Break") {
                throw new BeatmapParsingException("This line is not a break.", line);
            }

            EventType = values[0];

            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of break.", line);

            if (TryParseDouble(values[2], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of break.", line);
        }
    }
}
