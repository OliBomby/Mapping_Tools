using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LastAnchorGenerator : RelevantObjectsGenerator {
        public override string Name => "Virtual Points on the Last Anchors of Sliders";
        public override string Tooltip => "Generates virtual points on the last anchors of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider ? new RelevantPoint(ho.CurvePoints.Last()) : null;
        }
    }
}
