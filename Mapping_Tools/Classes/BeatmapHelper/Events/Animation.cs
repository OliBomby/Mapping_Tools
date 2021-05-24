using System;
using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Animation : Event, IHasDuration {
        public StoryboardLayer Layer { get; set; }
        public Origin Origin { get; set; }

        /// <summary>
        /// This is a partial path to the image file for this sprite.
        /// </summary>
        public string FilePath { get; set; }

        public Vector2 Pos { get; set; }

        public int FrameCount { get; set; }
        public double FrameDelay { get; set; }
        public LoopType LoopType { get; set; }

        /// <summary>
        /// Serializes this object to .osu code.
        /// </summary>
        /// <returns></returns>
        public override string GetLine() {
            return $"Animation,{Layer},{Origin},\"{FilePath}\",{Pos.X.ToRoundInvariant()},{Pos.Y.ToRoundInvariant()},{FrameCount.ToInvariant()},{FrameDelay.ToInvariant()},{LoopType}";
        }

        /// <summary>
        /// Deserializes a string of .osu code and populates the properties of this object.
        /// </summary>
        /// <param name="line"></param>
        public override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Animation") {
                throw new BeatmapParsingException("This line is not an animation.", line);
            }

            if (Enum.TryParse(values[1], out StoryboardLayer layer))
                Layer = layer;
            else throw new BeatmapParsingException("Failed to parse layer of animation.", line);

            if (Enum.TryParse(values[2], out Origin origin))
                Origin = origin;
            else throw new BeatmapParsingException("Failed to parse origin of animation.", line);

            FilePath = values[3].Trim('"');

            if (!TryParseDouble(values[4], out double x))
                throw new BeatmapParsingException("Failed to parse X position of animation.", line);

            if (!TryParseDouble(values[5], out double y))
                throw new BeatmapParsingException("Failed to parse Y position of animation.", line);

            Pos = new Vector2(x, y);

            if (TryParseInt(values[6], out int frameCount))
                FrameCount = frameCount;
            else throw new BeatmapParsingException("Failed to parse frame count of animation.", line);

            if (TryParseDouble(values[7], out double frameDelay))
                FrameDelay = frameDelay;
            else throw new BeatmapParsingException("Failed to parse frame delay of animation.", line);

            if (Enum.TryParse(values[8], out LoopType loopType))
                LoopType = loopType;
            else throw new BeatmapParsingException("Failed to parse loop type of animation.", line);
        }

        public double Duration { 
            get => FrameDelay;
            set => FrameDelay = value;
        }
    }
}