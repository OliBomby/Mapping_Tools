using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class LinearLineGenerator : RelevantObjectsGenerator, IGenerateLinesFromHitObjects {
        public override string Name => "Line Generator for Linear Sliders";
        public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantLine> GetRelevantObjects(List<HitObject> objects) {
            return (from ho in objects where ho.IsSlider && ho.SliderType == PathType.Linear && ho.CurvePoints.Count >= 1 select new RelevantLine(new Line2(ho.Pos, ho.CurvePoints[ho.CurvePoints.Count - 1]))).ToList();
        }
    }
}
