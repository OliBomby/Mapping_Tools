using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mapping_Tools {
    class Colour {
        public int Index { get; set; }
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }

        public Colour(int i, double r, double g, double b) {
            Index = i;
            Red = r;
            Green = g;
            Blue = b;
        }

        public Colour(string line) {
            string[] split = line.Split(':');
            Index = int.Parse(Regex.Match(split[0], @"\d+").Value);
            string[] csplit = split[1].Split(',');
            Red = double.Parse(csplit[0]);
            Green = double.Parse(csplit[1]);
            Blue = double.Parse(csplit[2]);
        }

        public string GetLine() {
            return "Combo" + Index + " : " + Math.Round(Red) + "," + Math.Round(Green) + "," + Math.Round(Blue);
        }
    }
}
