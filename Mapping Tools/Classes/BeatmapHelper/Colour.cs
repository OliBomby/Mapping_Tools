using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling. 
    /// </summary>
    public class Colour
    {
        /// <summary>
        /// The red value of the colour.
        /// </summary>
        public double Red { get; set; }
        /// <summary>
        /// The green value of the colour.
        /// </summary>
        public double Green { get; set; }
        /// <summary>
        /// The blue value of the colour.
        /// </summary>
        public double Blue { get; set; }

        /// <inheritdoc />
        public Colour(double r, double g, double b) {
            Red = r;
            Green = g;
            Blue = b;
        }

        /// <inheritdoc />
        public Colour(string line) {
            string[] split = line.Split(':');
            string[] commaSplit = split[1].Split(',');

            if (TryParseDouble(commaSplit[0], out double r))
                Red = r;
            else throw new BeatmapParsingException("Failed to parse red component of colour.", line);

            if (TryParseDouble(commaSplit[1], out double g))
                Green = g;
            else throw new BeatmapParsingException("Failed to parse green component of colour.", line);

            if (TryParseDouble(commaSplit[2], out double b))
                Blue = b;
            else throw new BeatmapParsingException("Failed to parse blue component of colour.", line);
        }


        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return $"{Red.ToRoundInvariant()},{Green.ToRoundInvariant()},{Blue.ToRoundInvariant()}";
        }
    }
}
