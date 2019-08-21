using System;

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
            Red = double.Parse(csplit[0]);
            Green = double.Parse(csplit[1]);
            Blue = double.Parse(csplit[2]);
        }

        public override string ToString() {
            return (int)Math.Round(Red) + "," + (int)Math.Round(Green) + "," + (int)Math.Round(Blue);
        }
    }
}
