using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class PerfectCircleBlanketGenerator : RelevantObjectsGenerator, IGeneratePointsFromHitObjects {
        public override string Name => "Three Point Slider Blanket Center Generator";
        public override string Tooltip => "Takes a circular arc slider and generates a virtual point on its blanket center.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantPoint> GetRelevantObjects(List<HitObject> objects) {
            return (from ho in objects
                where ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                select new Circle(new CircleArc(ho.GetAllCurvePoints()))
                into circle
                select new RelevantPoint(circle.Centre)).ToList();
        }
    }
}
