using Mapping_Tools.Classes.MathUtil;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools
{
    struct CircleBezierPreset
    {
        public double MaxAngle;
        public List<Vector2> Points;

        public CircleBezierPreset(double maxAngle, List<Vector2> points)
        {
            MaxAngle = maxAngle;
            Points = points;
        }

        public CircleBezierPreset(float maxAngle, List<Vector2> points)
        {
            MaxAngle = maxAngle;
            Points = points;
        }
    }
}
