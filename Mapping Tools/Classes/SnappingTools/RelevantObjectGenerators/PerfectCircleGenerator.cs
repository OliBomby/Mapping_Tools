using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    class PerfectCircleGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public string Name => "Perfect Circle Generator";
        public GeneratorType GeneratorType => GeneratorType.Basic;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || !(ho.SliderType == PathType.PerfectCurve))
                    continue;

                Circle circle = new Circle(new CircleArc(ho.GetAllCurvePoints()));
                newObjects.Add(new RelevantCircle(circle));
            }

            return newObjects;
        }
    }
}
