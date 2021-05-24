using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LinearLineGenerator : RelevantObjectsGenerator {
        public override string Name => "Lines on Linear Sliders";
        public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public LinearLineGenerator() {
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.Linear && ho.CurvePoints.Count >= 1
                ? new RelevantLine(Line2.FromPoints(ho.Pos, ho.CurvePoints.Last()))
                : null;
        }
    }
}
