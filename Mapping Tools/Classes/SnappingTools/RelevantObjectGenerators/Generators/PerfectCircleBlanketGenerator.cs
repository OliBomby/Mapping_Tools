using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class PerfectCircleBlanketGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public override string Name => "Three Point Slider Blanket Center Generator";
        public override string Tooltip => "Takes a circular arc slider and generates a virtual point on its blanket center.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || ho.SliderType != PathType.PerfectCurve)
                    continue;

                var circle = new CircleArc(ho.GetAllCurvePoints());
                newObjects.Add(new RelevantPoint(circle.Centre));
            }

            return newObjects;
        }
    }
}
