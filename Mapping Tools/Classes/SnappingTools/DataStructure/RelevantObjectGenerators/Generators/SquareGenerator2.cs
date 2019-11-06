using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SquareGenerator2 : RelevantObjectsGenerator {
        public override string Name => "Square from Two Points (Type II)";
        public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual points on each side to make two squares in total.";
        public override GeneratorType GeneratorType => GeneratorType.Assistants;

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var diff = point2.Child - point1.Child;
            var rotated = Vector2.Rotate(diff, Math.PI / 2);

            return new[] {
                new RelevantPoint(point1.Child - rotated),
                new RelevantPoint(point1.Child + rotated),
                new RelevantPoint(point2.Child - rotated),
                new RelevantPoint(point2.Child + rotated)
            };
        }
    }
}
