using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class PerfectCircleBlanketGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "Blanket Center Assistant";
        public new string Tooltip => "Takes a circular arc slider and generates a virtual point on its blanket center.";
        public new GeneratorType GeneratorType => GeneratorType.Assistants;

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
