using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SquareGenerator : RelevantObjectsGenerator {
        public override string Name => "Square from Two Points (Type I)";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make a single square.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var diff = point2.Child - point1.Child;
            var rotated = Vector2.Rotate(diff, Math.PI * 3 / 4) / Math.Sqrt(2);

            return new[] {
                new RelevantPoint(point1.Child - rotated),
                new RelevantPoint(point2.Child + rotated)
            };
        }
    }
}
