using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class PerfectCircleGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "Circle Generator";
        public new string Tooltip => "Takes a circular arc slider and generates a virtual circle that completes the arc.";
        public new GeneratorType GeneratorType => GeneratorType.Generators;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || ho.SliderType != PathType.PerfectCurve || ho.CurvePoints.Count != 2)
                    continue;

                Circle circle = new Circle(new CircleArc(ho.GetAllCurvePoints()));
                newObjects.Add(new RelevantCircle(circle));
            }

            return newObjects;
        }
    }
}
