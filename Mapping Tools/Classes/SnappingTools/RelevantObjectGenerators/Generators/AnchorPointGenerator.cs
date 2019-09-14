using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class AnchorPointGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "Anchor of Slider Point Generator";
        public new string Tooltip => "Generates virtual points on anchors of sliders.";
        public new GeneratorType GeneratorType => GeneratorType.Generators;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects)
        {
            return objects.Where(ho => ho.IsSlider)
                .SelectMany(ho => ho.GetAllCurvePoints().Select(poi => new RelevantPoint(poi))).Cast<IRelevantObject>()
                .ToList();
        }
    }
}
