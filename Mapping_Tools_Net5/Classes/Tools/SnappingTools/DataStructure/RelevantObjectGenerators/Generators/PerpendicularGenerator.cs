using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerpendicularGenerator : RelevantObjectsGenerator {
        public override string Name => "Perpendicular Lines";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is perpendicular to the line.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public PerpendicularGenerator() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point) {
            return new RelevantLine(new Line2(point.Child, line.Child.DirectionVector.PerpendicularLeft));
        }
    }
}
