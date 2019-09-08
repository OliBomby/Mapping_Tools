using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class LinearLineGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "Linear Line Generator";
        public new GeneratorType GeneratorType => GeneratorType.Basic;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || ho.SliderType != PathType.Linear)
                    continue;

                Line line = new Line(ho.Pos, ho.CurvePoints[ho.CurvePoints.Count - 1]);
                newObjects.Add(new RelevantLine(line));
            }

            return newObjects;
        }
    }
}
