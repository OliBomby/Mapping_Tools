using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LastAnchorGenerator : RelevantObjectsGenerator {
        public override string Name => "Points on Last Anchors";
        public override string Tooltip => "Generates virtual points on the last anchors of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public LastAnchorGenerator() {
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider ? new RelevantPoint(ho.CurvePoints.Last()) : null;
        }
    }
}
