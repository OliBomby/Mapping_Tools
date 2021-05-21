using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class EqualSpacingGenerator : RelevantObjectsGenerator {
        public override string Name => "Circles by Two Points";
        public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual circles with their centers on each point. Their radius is equal to the spacing between the two.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public EqualSpacingGenerator() {
            Settings.IsActive = true;
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var radius = (point1.Child - point2.Child).Length;
            return new[] {
                new RelevantCircle(new Circle(point1.Child, radius)),
                new RelevantCircle(new Circle(point2.Child, radius))
            };
        }
    }
}
