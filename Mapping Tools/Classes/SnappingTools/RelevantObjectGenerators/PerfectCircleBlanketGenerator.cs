using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    class PerfectCircleBlanketGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public string Name => "Perfect Circle Blanket Generator";
        public GeneratorType GeneratorType => GeneratorType.Blankets;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || !(ho.SliderType == PathType.PerfectCurve))
                    continue;

                var circle = new CircleArc(ho.GetAllCurvePoints());
                newObjects.Add(new RelevantPoint(circle.Centre));
            }

            return newObjects;
        }
    }
}
