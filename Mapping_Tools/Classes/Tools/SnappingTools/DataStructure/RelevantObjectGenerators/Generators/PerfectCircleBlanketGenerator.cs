using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerfectCircleBlanketGenerator : RelevantObjectsGenerator {
        public override string Name => "Points on Blanket Centers";
        public override string Tooltip => "Takes a circular arc slider and generates a virtual point on its blanket center.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public PerfectCircleBlanketGenerator() {
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                ? new RelevantPoint(new Circle(new CircleArc(ho.GetAllCurvePoints())).Centre)
                : null;
        }
    }
}
