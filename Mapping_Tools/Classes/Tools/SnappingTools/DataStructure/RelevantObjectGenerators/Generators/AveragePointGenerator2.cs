using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AveragePointGenerator2 : RelevantObjectsGenerator {
        public override string Name => "Average of Two Points";
        public override string Tooltip => "Takes a pair of virtual points and calculates the average of the points.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public AveragePointGenerator2() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            return new RelevantPoint((point1.Child + point2.Child) / 2);
        }
    }
}
