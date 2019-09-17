using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class LinearLineGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public override string Name => "Line Generator for Linear Sliders";
        public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                // Only get perfect type sliders
                if (!ho.IsSlider || ho.SliderType != PathType.Linear || ho.CurvePoints.Count < 1)
                    continue;

                Line line = new Line(ho.Pos, ho.CurvePoints[ho.CurvePoints.Count - 1]);
                newObjects.Add(new RelevantLine(line));
            }

            return newObjects;
        }
    }
}
