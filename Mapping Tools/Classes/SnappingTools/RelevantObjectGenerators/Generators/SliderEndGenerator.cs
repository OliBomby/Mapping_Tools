using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class SliderEndGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "Slider End Generator";
        public new string Tooltip => "Generates virtual points on the actual ends of sliders.";
        public new GeneratorType GeneratorType => GeneratorType.Generators;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects)
        {
            return (from ho in objects where ho.IsSlider select new RelevantPoint(ho.GetSliderPath().PositionAt(1))).Cast<IRelevantObject>().ToList();
        }
    }
}
