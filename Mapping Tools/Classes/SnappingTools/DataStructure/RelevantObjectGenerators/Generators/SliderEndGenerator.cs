using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class SliderEndGenerator : RelevantObjectsGenerator, IGeneratePointsFromHitObjects {
        public override string Name => "Virtual Points on Slider Ends";
        public override string Tooltip => "Generates virtual points on the actual ends of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantPoint> GetRelevantObjects(List<HitObject> objects)
        {
            return (from ho in objects where ho.IsSlider select new RelevantPoint(ho.GetSliderPath().PositionAt(1))).ToList();
        }
    }
}
