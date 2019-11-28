using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerfectCircleGenerator : RelevantObjectsGenerator {
        public override string Name => "Circles on 3-Point Sliders";
        public override string Tooltip => "Takes a circular arc slider and generates a virtual circle that completes the arc.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                ? new RelevantCircle(new Circle(new CircleArc(ho.GetAllCurvePoints())))
                : null;
        }
    }
}
