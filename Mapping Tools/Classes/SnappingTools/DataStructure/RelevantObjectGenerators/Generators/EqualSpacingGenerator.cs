using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
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

                    var radius = (obj2.Child - obj1.Child).Length;
                    newObjects.Add(new RelevantCircle(new Circle(obj1.Child, radius)));
                    newObjects.Add(new RelevantCircle(new Circle(obj2.Child, radius)));
                }
            }

            return newObjects;
        }
    }
}
