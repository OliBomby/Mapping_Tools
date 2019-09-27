using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class AnchorPointGenerator : RelevantObjectsGenerator, IGeneratePointsFromHitObjects {
        public override string Name => "Virtual Points on Slider Anchors";
        public override string Tooltip => "Generates virtual points on the anchor points of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantPoint> GetRelevantObjects(List<HitObject> objects) {
            return objects.Where(ho => ho.IsSlider)
                .SelectMany(ho => ho.GetAllCurvePoints().Select(poi => new RelevantPoint(poi))).ToList();
        }
    }
}
