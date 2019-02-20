using System.Collections.Generic;
using System.Globalization;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SliderPathStuff {
    class SliderInterpreter {
        public static SliderPath InterpretSlider(string line) {
            string[] split = line.Split(',');
            string[] sliderData = split[5].Split('|');
            List<Vector2> points = new List<Vector2>
            {
                new Vector2(double.Parse(split[0]), double.Parse(split[1]))
            };
            for( int i = 1; i < sliderData.Length; i++ ) {
                points.Add(new Vector2(double.Parse(sliderData[i].Split(':')[0]), double.Parse(sliderData[i].Split(':')[1])));
            }
            PathType type = PathType.Linear;
            string typeString = sliderData[0].ToString();
            if( typeString == "B" ) {
                type = PathType.Bezier;
            }
            else if( typeString == "P" ) {
                type = PathType.PerfectCurve;
            }
            else if( typeString == "C" ) {
                type = PathType.Catmull;
            }
            double pixelLength = double.Parse(split[7], CultureInfo.InvariantCulture);
            if( pixelLength > 0 ) {
                return new SliderPath(type, points.ToArray(), pixelLength);
            }
            else {
                return new SliderPath(type, points.ToArray());
            }
        }
    }
}
