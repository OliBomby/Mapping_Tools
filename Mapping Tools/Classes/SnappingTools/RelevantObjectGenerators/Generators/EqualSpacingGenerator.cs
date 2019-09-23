using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class EqualSpacingGenerator : RelevantObjectsGenerator, IGenerateCirclesFromRelevantObjects {
        public override string Name => "Assistant for Equal Spacing defined by Two Points";
        public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual circles with their centers on each point. Their radius is equal to the spacing between the two.";
        public override GeneratorType GeneratorType => GeneratorType.Assistants;

        public List<RelevantCircle> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            var newObjects = new List<RelevantCircle>();

            for (var i = 0; i < points.Count; i++) {
                for (var k = i + 1; k < points.Count; k++) {
                    var obj1 = points[i];
                    var obj2 = points[k];

                    var radius = (obj2.child - obj1.child).Length;
                    newObjects.Add(new RelevantCircle(new Circle(obj1.child, radius)));
                    newObjects.Add(new RelevantCircle(new Circle(obj2.child, radius)));
                }
            }

            return newObjects;
        }
    }
}
