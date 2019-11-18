using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PointBisectorGenerator : RelevantObjectsGenerator {
        public override string Name => "Bisector of Two Points";
        public override string Tooltip => "Takes a pair virtual points and generates the bisector of those points.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public PointBisectorGenerator() {
            Settings.IsSequential = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            return new RelevantLine(new Line2((point1.Child + point2.Child) / 2, (point2.Child - point1.Child).PerpendicularLeft));
        }
    }
}
