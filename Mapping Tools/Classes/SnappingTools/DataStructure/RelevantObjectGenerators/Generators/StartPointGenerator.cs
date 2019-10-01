using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class StartPointGenerator : RelevantObjectsGenerator {
        public override string Name => "Virtual Points on Slider Heads and Circles";
        public override string Tooltip => "Generates virtual points on slider heads and circles.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        [RelevantObjectGenerator(typeof(RelevantHitObject), typeof(RelevantPoint))]
        public RelevantPoint GetRelevantObjects(RelevantHitObject ho) {
            return new RelevantPoint(ho.HitObject.Pos);
        }
    }
}
