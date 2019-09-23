using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class IntersectionGenerator : RelevantObjectsGenerator, IGeneratePointsFromRelevantObjects {
        public override string Name => "Intersection Point Calculator";
        public override string Tooltip => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<RelevantPoint> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            var newObjects = new List<RelevantPoint>();

            // Intersect lines with lines
            for (var i = 0; i < lines.Count; i++) {
                for (var k = i + 1; k < lines.Count; k++) {
                    var obj1 = lines[i];
                    var obj2 = lines[k];

                    if (!Line2.Intersection(obj1.child, obj2.child, out var intersection)) continue;
                    newObjects.Add(new RelevantPoint(intersection));
                }
            }

            // Intersect lines with circles
            foreach (var line in lines) {
                foreach (var circle in circles) {
                    if (!Circle.Intersection(circle.child, line.child, out var intersections)) continue;
                    newObjects.AddRange(intersections.Select(v => new RelevantPoint(v)));
                }
            }

            // Intersect circles with circles
            for (var i = 0; i < circles.Count; i++) {
                for (var k = i + 1; k < circles.Count; k++) {
                    var obj1 = circles[i];
                    var obj2 = circles[k];

                    if (!Circle.Intersection(obj1.child, obj2.child, out var intersections)) continue;
                    newObjects.AddRange(intersections.Select(v => new RelevantPoint(v)));
                }
            }

            return newObjects;
        }
    }
}
