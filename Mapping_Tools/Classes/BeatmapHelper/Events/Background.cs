using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Background : Event {
        public string EventType { get; set; }
        public int StartTime { get; set; }
        public string Filename { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }

        public Vector2 GetOffset() {
            return new Vector2(XOffset, YOffset);
        }

        public override string GetLine() {
            // Writing the offset is optional if its 0,0 but we add it anyways because that is what osu! does.
            return $"{EventType},{StartTime.ToInvariant()},\"{Filename}\",{XOffset.ToInvariant()},{YOffset.ToInvariant()}";
        }

        public override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "0") {
                throw new BeatmapParsingException("This line is not a background.", line);
            }

            EventType = values[0];

            // This start time is usually 0 for backgrounds but lets parse it anyways
            if (TryParseInt(values[1], out int startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of background.", line);

            Filename = values[2].Trim('"');

            // Writing offset is optional
            if (values.Length > 3) {
                if (TryParseInt(values[3], out int xOffset))
                    XOffset = xOffset;
                else throw new BeatmapParsingException("Failed to parse X offset of background.", line);

                if (TryParseInt(values[4], out int yOffset))
                    YOffset = yOffset;
                else throw new BeatmapParsingException("Failed to parse Y offset of background.", line);
            } else {
                XOffset = 0;
                YOffset = 0;
            }
        }
    }
}