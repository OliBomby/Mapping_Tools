using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class PerpendicularGenerator : RelevantObjectsGenerator, IGenerateLinesFromRelevantObjects {
        public override string Name => "Perpendicular Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line perpendicular to the line on top of the point.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<IRelevantObject> GetRelevantObjects(List<IRelevantObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    if (obj1 is RelevantLine line && obj2 is RelevantPoint point)
                    {
                        newObjects.Add(MakePerpendicularLine(line.child, point.child));
                    } else
                    if (obj2 is RelevantLine line2 && obj1 is RelevantPoint point2) {
                        newObjects.Add(MakePerpendicularLine(line2.child, point2.child));
                    }
                }
            }

            return newObjects;
        }

        private static RelevantLine MakePerpendicularLine(Line line, Vector2 point)
        {
            var a = -line.B;
            var b = line.A;
            return new RelevantLine(new Line(a, b, a * point.X + b * point.Y));
        }

        public List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            return (from line in lines from point in points select MakePerpendicularLine(line.child, point.child)).ToList();
        }
    }
}
