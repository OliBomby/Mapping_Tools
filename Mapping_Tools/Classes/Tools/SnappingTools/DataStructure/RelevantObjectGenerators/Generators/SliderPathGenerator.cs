﻿using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SliderPathGenerator : RelevantObjectsGenerator {
        public override string Name => "Points on Slider Paths";
        public override string Tooltip => "Generates many virtual points on the paths of sliders. The density of generated points is configurable.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

        /// <summary>
        /// Initializes SliderPathGenerator with a custom settings object
        /// </summary>
        public SliderPathGenerator() : base(new SliderPathGeneratorSettings()) {
            Settings.Generator = this;

            Settings.RelevancyRatio = 0.6;
            Settings.GeneratesInheritable = false;
            ((SliderPathGeneratorSettings) Settings).PointDensity = 0.5;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint[] GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            if (!ho.IsSlider) {
                return null;
            }

            var numPoints = (int)(ho.PixelLength * ((SliderPathGeneratorSettings)Settings).PointDensity);
            var points = new RelevantPoint[numPoints];
            var sliderPath = ho.GetSliderPath();
            
            for (int i = 0; i < numPoints; i++) {
                points[i] = new RelevantPoint(sliderPath.PositionAt((double)i / (numPoints - 1))) {
                    CustomTime = (double)i / (numPoints - 1) * (ho.EndTime - ho.Time) + ho.Time
                };
            }

            return points;
        }
    }
}
