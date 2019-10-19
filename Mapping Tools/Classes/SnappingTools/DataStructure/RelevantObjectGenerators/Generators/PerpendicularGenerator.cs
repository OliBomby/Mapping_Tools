using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerpendicularGenerator : RelevantObjectsGenerator {
        public override string Name => "Perpendicular Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is perpendicular to the line.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point) {
            return new RelevantLine(new Line2(point.Child, line.Child.DirectionVector.PerpendicularLeft));
        }
    }
}
