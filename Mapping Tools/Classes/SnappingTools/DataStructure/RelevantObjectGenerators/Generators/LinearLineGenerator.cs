using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class LinearLineGenerator : RelevantObjectsGenerator, IGenerateLinesFromHitObjects {
        public override string Name => "Virtual Lines on Linear Sliders";
        public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantLine> GetRelevantObjects(List<HitObject> objects) {
            return (from ho in objects where ho.IsSlider && ho.SliderType == PathType.Linear && ho.CurvePoints.Count >= 1 select new RelevantLine(Line2.FromPoints(ho.Pos, ho.CurvePoints[ho.CurvePoints.Count - 1]))).ToList();
        }
    }
}
