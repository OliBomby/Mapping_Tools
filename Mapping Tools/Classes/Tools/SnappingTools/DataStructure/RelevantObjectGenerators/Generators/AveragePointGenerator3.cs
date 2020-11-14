using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AveragePointGenerator3 : RelevantObjectsGenerator {
        public override string Name => "Average of Three Points";
        public override string Tooltip => "Takes three virtual points and calculates the average of the points.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public AveragePointGenerator3() {
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3) {
            return new RelevantPoint((point1.Child + point2.Child + point3.Child) / 3);
        }
    }
}
