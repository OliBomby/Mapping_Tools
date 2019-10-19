using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class AnchorPointGenerator : RelevantObjectsGenerator {
        public override string Name => "Virtual Points on Slider Anchors";
        public override string Tooltip => "Generates virtual points on the anchor points of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        [RelevantObjectGenerator]
        public IEnumerable<RelevantPoint> GetRelevantObjects(RelevantHitObject relevantHitObject) {
            return relevantHitObject.HitObject.IsSlider
                ? relevantHitObject.HitObject.GetAllCurvePoints().Select(o => new RelevantPoint(o))
                : new RelevantPoint[0];
        }
    }
}
