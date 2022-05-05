using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SliderEndGenerator : RelevantObjectsGenerator {
        public override string Name => "Points on Slider Ends";
        public override string Tooltip => "Generates virtual points on the actual ends of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

        public SliderEndGenerator() {
            Settings.RelevancyRatio = 0.8;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider ? new RelevantPoint(ho.GetSliderPath().PositionAt(1)) { CustomTime = ho.EndTime } : null;
        }
    }
}
