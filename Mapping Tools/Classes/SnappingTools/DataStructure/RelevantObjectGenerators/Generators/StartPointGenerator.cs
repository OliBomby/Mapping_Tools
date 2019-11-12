using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class StartPointGenerator : RelevantObjectsGenerator {
        public override string Name => "Virtual Points on Slider Heads and Circles";
        public override string Tooltip => "Generates virtual points on slider heads and circles.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject ho) {
            return new RelevantPoint(ho.HitObject.Pos);
        }
    }
}
