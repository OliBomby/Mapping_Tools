using System;
using System.Globalization;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class Colour
    {
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }

        public Colour(double r, double g, double b) {
            Red = r;
            Green = g;
            Blue = b;
        }

        public Colour(string line) {
            string[] split = line.Split(':');
            string[] csplit = split[1].Split(',');

            if (TryParseDouble(csplit[0], out double r))
                Red = r;
            else throw new BeatmapParsingException("Failed to parse red component of colour.", line);

            if (TryParseDouble(csplit[1], out double g))
                Green = g;
            else throw new BeatmapParsingException("Failed to parse green component of colour.", line);

            if (TryParseDouble(csplit[2], out double b))
                Blue = b;
            else throw new BeatmapParsingException("Failed to parse blue component of colour.", line);
        }

        private bool TryParseDouble(string d, out double result) {
            return double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public override string ToString() {
            return (int)Math.Round(Red) + "," + (int)Math.Round(Green) + "," + (int)Math.Round(Blue);
        }
    }
}
