using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class IntersectionGenerator : RelevantObjectsGenerator {
        public override string Name => "Intersection Points";
        public override string Tooltip => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;

        public IntersectionGenerator() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetLineLineIntersection(RelevantLine line1, RelevantLine line2) {
            return Line2.Intersection(line1.Child, line2.Child, out var intersection) ? new RelevantPoint(intersection) : null;
        }

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetLineCircleIntersection(RelevantLine line, RelevantCircle circle) {
            return Circle.Intersection(circle.Child, line.Child, out var intersections) ? intersections.Select(o => new RelevantPoint(o)) : null;
        }

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetCircleCircleIntersection(RelevantCircle circle1, RelevantCircle circle2) {
            return Circle.Intersection(circle1.Child, circle2.Child, out var intersections) ? intersections.Select(o => new RelevantPoint(o)) : null;
        }
    }
}
