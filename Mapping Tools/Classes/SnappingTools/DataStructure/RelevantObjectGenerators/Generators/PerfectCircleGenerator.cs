﻿using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class PerfectCircleGenerator : RelevantObjectsGenerator, IGenerateCirclesFromHitObjects {
        public override string Name => "Virtual Circles on 3-Point Sliders";
        public override string Tooltip => "Takes a circular arc slider and generates a virtual circle that completes the arc.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantCircle> GetRelevantObjects(List<HitObject> objects) {
            return (from ho in objects
                where ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                select new Circle(new CircleArc(ho.GetAllCurvePoints()))
                into circle
                select new RelevantCircle(circle)).ToList();
        }
    }
}
