using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Background : Event, IHasStartTime {
        public string EventType { get; set; }
        public double StartTime { get; set; }
        public string Filename { get; set; }

        public Vector2 Pos { get; set; }

        public override string GetLine() {
            // Writing the offset is optional if its 0,0 but we add it anyways because that is what osu! does.
            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},\"{Filename}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()}";
        }

        public override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "0") {
                throw new BeatmapParsingException("This line is not a background.", line);
            }

            EventType = values[0];

            // This start time is usually 0 for backgrounds but lets parse it anyways
            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of background.", line);

            Filename = values[2].Trim('"');

            // Writing offset is optional
            if (values.Length > 3) {
                if (!TryParseDouble(values[3], out double xOffset))
                   throw new BeatmapParsingException("Failed to parse X offset of background.", line);

                if (!TryParseDouble(values[4], out double yOffset))
                   throw new BeatmapParsingException("Failed to parse Y offset of background.", line);

                Pos = new Vector2(xOffset, yOffset);
            } else {
                Pos = Vector2.Zero;
            }
        }
    }
}