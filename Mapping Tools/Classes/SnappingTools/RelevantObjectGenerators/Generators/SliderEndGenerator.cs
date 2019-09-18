using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class SliderEndGenerator : RelevantObjectsGenerator, IGeneratePointsFromHitObjects {
        public override string Name => "Slider End Generator";
        public override string Tooltip => "Generates virtual points on the actual ends of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantPoint> GetRelevantObjects(List<HitObject> objects)
        {
            return (from ho in objects where ho.IsSlider select new RelevantPoint(ho.GetSliderPath().PositionAt(1))).ToList();
        }
    }
}
