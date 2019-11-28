using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AngleBisectorGenerator : RelevantObjectsGenerator {
        public override string Name => "Bisectors of Angles";
        public override string Tooltip => "Takes a pair virtual lines and generates the bisector of the angle between those lines at the point of the intersection.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        [RelevantObjectsGeneratorMethod]
        public RelevantLine[] GetRelevantObjects(RelevantLine line1, RelevantLine line2) {
            if (!Line2.Intersection(line1.Child, line2.Child, out var intersection)) return null;
            var dir1Norm = Vector2.Normalize(line1.Child.DirectionVector);
            var dir2Norm = Vector2.Normalize(line2.Child.DirectionVector);
            return new[] {
                new RelevantLine(new Line2(intersection, dir1Norm + dir2Norm)), 
                new RelevantLine(new Line2(intersection, dir1Norm - dir2Norm))
            };
        }
    }
}
