﻿using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AnchorPointGenerator : RelevantObjectsGenerator {
        public override string Name => "Points on Slider Anchors";
        public override string Tooltip => "Generates virtual points on the anchor points of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider ? ho.GetAllCurvePoints().Select(o => new RelevantPoint(o)) : null;
        }
    }
}
