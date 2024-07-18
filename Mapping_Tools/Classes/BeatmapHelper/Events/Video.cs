using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Video : Event, IHasStartTime {
        public string EventType { get; set; }
        public double StartTime { get; set; }
        public string Filename { get; set; }
        public Vector2 Pos { get; set; }

        public override string GetLine() {
            // Dont write the offset if its 0,0
            if (Pos == Vector2.Zero) {
                return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},\"{Filename}\"";
            }

            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},\"{Filename}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()}";
        }

        public override void SetLine(string line) {
            string[] values = line.Split(',');

            // Either 'Video' or '1' indicates a video. We save the value so we dont accidentally change it.
            if (values[0] != "1" && values[0] != "Video") {
                throw new BeatmapParsingException("This line is not a video.", line);
            }

            EventType = values[0];

            // This start time is usually 0 for backgrounds but lets parse it anyways
            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of video.", line);

            Filename = values[2].Trim('"');

            // Writing offset is optional
            if (values.Length > 3) {
                if (!TryParseDouble(values[3], out double xOffset))
                    throw new BeatmapParsingException("Failed to parse X offset of video.", line);

                if (!TryParseDouble(values[4], out double yOffset))
                    throw new BeatmapParsingException("Failed to parse Y offset of video.", line);

                Pos = new Vector2(xOffset, yOffset);
            } else {
                Pos = Vector2.Zero;
            }
        }
    }
}